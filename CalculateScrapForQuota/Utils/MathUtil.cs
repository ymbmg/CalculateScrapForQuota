using System;
using System.Collections.Generic;

namespace CalculateScrapForQuota.Utils;

public class MathUtil
{
    public static (List<T> combination, int totalValue) FindBestCombination<T>(List<T> items, int quota, Func<T, int> value)
    {
        int n = items.Count;
        int[,] dp = new int[n + 1, quota + 1];

        for (int i = 0; i <= n; i++)
        {
            for (int j = 0; j <= quota; j++)
            {
                if (i == 0 || j == 0)
                    dp[i, j] = 0;
                else
                {
                    int itemValue = value(items[i - 1]);
                    if (itemValue <= j)
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i - 1, j - itemValue] + itemValue);
                    else
                        dp[i, j] = dp[i - 1, j];
                }
            }
        }

        // Backtrack to find the items
        var result = new List<T>();
        int res = dp[n, quota];
        int w = quota;
        for (int i = n; i > 0 && res > 0; i--)
        {
            if (res != dp[i - 1, w])
            {
                result.Add(items[i - 1]);
                res -= value(items[i - 1]);
                w -= value(items[i - 1]);
            }
        }

        int totalValue = dp[n, quota];
        return (result, totalValue);
    }
}