using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceSimulator.Framing
{
    /// <summary>
    /// 【分帧层】把零碎字节流切成一条条完整命令。
    /// 解决半包（数据没到齐）和粘包（一次来好几条）。
    /// 算法：内存蓄水池 + 查找定界符 CRLF。
    /// </summary>
    public sealed class DelimiterFrameDecoder
    {
        // 内存中的累积蓄水池
        private readonly List<byte> _buffer = new List<byte>();

        // 约定的结束定界符（默认 \r\n，即 0x0D 0x0A）
        private readonly byte[] _delimiter;

        // 缓冲区最大保护容量（防止极端干扰导致内存涨爆）
        private readonly int _maxBufferSize;

        // 构造函数，允许自定义定界符和最大缓冲区大小
        public DelimiterFrameDecoder(byte[]? delimiter = null, int maxBufferSize = 4096)
        {
            _delimiter = delimiter ?? new byte[] { 0x0D, 0x0A };
            _maxBufferSize = maxBufferSize;
        }

        /// <summary>
        /// 喂入碎片裸字节，返回本次能切出的所有完整帧。
        /// 数据没凑齐时返回空列表。
        /// </summary>
        public List<byte[]> Feed(byte[] incomingChunk)
        {
            var completedFrames = new List<byte[]>();       // 用于存储本次能切出的完整帧

            //校验传入的碎片是否为空
            if (incomingChunk == null || incomingChunk.Length == 0)
            {
                return completedFrames; // 如果传入的碎片为空，直接返回空列表
            }

            // 1. 新数据无脑追加到蓄水池
            _buffer.AddRange(incomingChunk);

            // 2. 循环扫描：用 while 解决粘包（一次粘 3 包就连续切 3 次）
            while (true)
            {
                // 在蓄水池中查找定界符的位置:  查找 0x0D, 0x0A 的索引位置
                int delimiterIndex = IndexOfSubArray(_buffer, _delimiter);

                // 找不到定界符 = 半包，留着等下一次
                if(delimiterIndex < 0)
                {
                    break; // 停止扫描，跳出循环
                }

                // 计算出包含定界符在内的整帧报文总长度
                int frameLength = delimiterIndex + _delimiter.Length;

                // 3. 截取并复制这包完整数据
                byte[] frame = _buffer.Take(frameLength).ToArray();
                completedFrames.Add(frame);             // 收集本次喂入后成功切出的完整帧

                // 4. 精准切除：只移除刚刚已消费掉的这帧数据！
                //    后面粘连进来的后续字节自动前移，保留在蓄水池中！
                _buffer.RemoveRange(0, frameLength);
            }

            // 5. 异常防护：如果缓冲区严重超限仍未找到定界符，强行清空防止 OOM
            if (_buffer.Count > _maxBufferSize)
            {
                Console.WriteLine($"[分帧告警] 缓冲区超过 {_maxBufferSize} 字节仍无定界符，已清空。");
                _buffer.Clear();
            }

            return completedFrames;     // 返回本次喂入后成功切出的完整帧列表
        }


        /// <summary>
        /// 在主集合中搜索子字节模式序列的算法
        /// </summary>
        private static int IndexOfSubArray(List<byte> source, byte[] pattern)
        {
            // 第 1 步：异常防护（边界检查）
            // 如果主集合的总长度连要找的目标特征串（pattern）都装不下，必定匹配失败，直接返回 -1
            if (source.Count < pattern.Length) return -1;

            // 第 2 步：外层循环控制——滑动窗口的“起始指针 i”
            // 注意循环终止条件：只要剩余未比对的字节长度小于 pattern.Length，就不可能再匹配成功
            for (int i = 0; i <= source.Count - pattern.Length; i++)
            {
                bool isMatch = true;        // 假设从当前位置 i 开始，后面能完全吻合
                                            // 第 3 步：内层循环——拿着模式串 pattern 的每一个字节，逐一与主数据比对
                for (int j = 0; j < pattern.Length; j++)
                {
                    // source[i + j]：主集合中从起点 i 开始，向后偏移 j 个位置的字节
                    // pattern[j]：特征串自身第 j 个字节
                    if (source[i + j] != pattern[j])
                    {
                        isMatch = false; // 只要有任何一个字节对不上，立即宣判本次匹配失败
                        break;           // 提前熔断跳出内层循环，不再做无意义的比对
                    }
                }
                // 第 4 步：检查内层循环的结果
                // 如果内循环完整跑完，且没有任何一个字节触发不相等，说明在此处找到了完整特征
                if (isMatch) return i;      // 立即返回匹配成功的起始索引 i
            }
            // 第 5 步：外层全部遍历完依然没有命中，说明主集合中不存在该模式串
            return -1;
        }
    }
}
