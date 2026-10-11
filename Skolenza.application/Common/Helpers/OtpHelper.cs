using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class OtpHelper
    {
        private static readonly Random _random = new();
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate6CharAlphanumericOtp()
        {
            return new string(Enumerable.Repeat(_chars, 6)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }

}
