using System;
using System.Collections.Generic;
using System.Linq;

namespace IotGatewayLearning.Domain.AI.Knowledge
{
    /// <summary>
    /// 关键词切分：中文没有空格，用 2-gram 近似切词；英文单词整体保留。
    /// 该结果只用于 MySQL LIKE 检索与排序，不冒充分词器。
    /// </summary>
    public static class KnowledgeTerms
    {
        private static readonly char[] Separators =
        {
            ' ', '\t', '\r', '\n',
            ',', '，', '。', '.', '?', '？', '!', '！',
            ';', '；', ':', '：', '、',
            '(', ')', '（', '）', '[', ']', '【', '】',
            '"', '\'', '“', '”', '‘', '’'
        };

        public static List<string> Split(string question, int limit = 8)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(question) || limit <= 0)
                return result;

            foreach (var segment in question.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (result.Count >= limit)
                    break;

                if (segment.Length <= 2 || segment.All(c => c < 128))
                {
                    Add(result, segment);
                    continue;
                }

                for (var i = 0; i + 1 < segment.Length && result.Count < limit; i++)
                    Add(result, segment.Substring(i, 2));
            }

            return result;
        }

        private static void Add(List<string> list, string term)
        {
            var value = term.Trim();
            if (value.Length == 0)
                return;

            if (!list.Contains(value, StringComparer.Ordinal))
                list.Add(value);
        }
    }
}
