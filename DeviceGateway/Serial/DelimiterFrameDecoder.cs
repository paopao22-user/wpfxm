using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotGatewayLearning.DeviceGateway.Serial
{
    /// <summary>
    /// 【分帧层】把零碎字节流切成一条条完整帧。
    /// 解决半包（数据没到齐）和粘包（一次来好几条）。
    /// </summary>
    public sealed class DelimiterFrameDecoder
    {
        private readonly List<byte> _buffer = new List<byte>();         // 内存中的累积蓄水池
        private readonly byte[] _delimiter;             // 约定的结束定界符（默认 \r\n，即 0x0D 0x0A）
        private readonly int _maxBufferSize;            // 缓冲区最大保护容量（防止极端干扰导致内存涨爆）

        public DelimiterFrameDecoder(byte[]? delimiter = null, int maxBufferSize = 4096)
        {
            _delimiter = delimiter ?? new byte[] { 0x0D, 0x0A };
            _maxBufferSize = maxBufferSize;
        }

        /// <summary>
        /// 喂入碎片字节，返回本次能切出的所有完整帧；没凑齐时返回空列表。
        /// </summary>
        /*
        1. 准备一个空的结果列表，用来装分帧
        2. 如果传进来的分帧没有内容，直接返回刚才的空结果列表
        3. 把新字节加到蓄水池里面去
        4. 在蓄水池里面找到边界符\r\n的位置
        5. 找不到 -> 数据还没到齐，跳出循环(半包）
        6. 找到了 -> 算出这帧总长度 = 定界符位置 + 定界符长度
        7. 从蓄水池开头截取这么长，作为一帧，加进结果列表
        8. 从蓄水池里面删除已切除的部分，保留后面未删除的部分
        9. 回到第四步，继续切
        10. 循环结束后蓄水池超过上限4096，清空它
        11. 返回结果列表

         */
        public List<byte[]> Feed(byte[] incomingChunk)
        {
            var completedFrames = new List<byte[]>();       // 用于存储本次能切出的完整帧

            if(incomingChunk == null || incomingChunk.Length == 0)
            {
                return completedFrames;
            }

            _buffer.AddRange(incomingChunk);    //将新字节加到蓄水池里面

            while (true)
            {
                int delimiterIndex = IndexOfSubArray(_buffer, _delimiter);      // 找到定界符的索引位置

                // 没找到索引位置，半包
                if(delimiterIndex < 0)
                {
                    break;
                }

                // 计算出包含定界符在内的整帧报文总长度
                int frameLength = delimiterIndex + _delimiter.Length;       // 整帧长度 = 索引位置 + 索引长度
                byte[] frame = _buffer.Take(frameLength).ToArray();     // 截取并复制这包完整数据
                completedFrames.Add(frame);     // 将截取出来的帧加进去存储帧

                _buffer.RemoveRange(0, frameLength);    // 从蓄水池里面删掉截取出去的完整帧
            }

            if(_buffer.Count > _maxBufferSize)
            {
                _buffer.Clear();
            }


            return completedFrames;         // 返回本次喂入后成功切出的完整帧
        }

        /// <summary>
        ///  在主集合中搜索子字节模式序列的算法
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /*
        1. 如果主数组比模式还短，直接返回-1
        2. 从主数组的第0个位置开始，逐个比较当可能的起点
        3. 对每个起点，逐个比较模式里面的字节(新开一个子循环)
        4. 只要有一个字节没对上，就标记失败，break
        5. 如果内层比较都成功了，说明已经找到位置了，标记成功，返回当层当做起点
        6. 所有起点都试过了还没有找到，返回-1
         */
        private static int IndexOfSubArray(List<byte> source, byte[] pattern)
        {
            if (source.Count < pattern.Length)
                return -1;

            for(int i = 0; i <= source.Count - pattern.Length; i++)
            {
                bool isMatch = true;
                for(int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch) return i;

            }
            return -1;
        }
    }
}
