using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Common.Helpers
{
    public static class AccountTypeHelper
    {
        public static readonly HashSet<string> ValidTypes =
            new(StringComparer.OrdinalIgnoreCase)
            { "Salary", "Current", "Savings" };

        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "saving"; // default

            input = input.Trim();

            if (input.Equals("saving", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("savings", StringComparison.OrdinalIgnoreCase))
                return "saving";

            if (input.Equals("salary", StringComparison.OrdinalIgnoreCase))
                return "salary";

            if (input.Equals("current", StringComparison.OrdinalIgnoreCase))
                return "current";

            throw new Exception("Invalid AccountType");
        }
    }
}
