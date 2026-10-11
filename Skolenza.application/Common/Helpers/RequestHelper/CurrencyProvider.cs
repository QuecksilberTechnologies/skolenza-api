using skolenza.application.DTOS.EnumDTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Common.Helpers.RequestHelper
{
    public static class CurrencyProvider
    {
        public static List<GetAllCurrencyResponseDTO> GetAll(bool isActive = true)
        {
            var list = new List<GetAllCurrencyResponseDTO>
        {
            new() { Id = 1, Code = "INR", Name = "Indian Rupee", Symbol = "₹", IsActive = true },
            new() { Id = 2, Code = "USD", Name = "US Dollar", Symbol = "$", IsActive = true },
            new() { Id = 3, Code = "EUR", Name = "Euro", Symbol = "€", IsActive = true },
            new() { Id = 4, Code = "GBP", Name = "British Pound", Symbol = "£", IsActive = true },
            new() { Id = 5, Code = "AED", Name = "UAE Dirham", Symbol = "د.إ", IsActive = true },
            new() { Id = 6, Code = "SGD", Name = "Singapore Dollar", Symbol = "$", IsActive = true },
            new() { Id = 7, Code = "AUD", Name = "Australian Dollar", Symbol = "$", IsActive = true },
            new() { Id = 8, Code = "CAD", Name = "Canadian Dollar", Symbol = "$", IsActive = true },
            new() { Id = 9, Code = "JPY", Name = "Japanese Yen", Symbol = "¥", IsActive = true },
            new() { Id = 10, Code = "CNY", Name = "Chinese Yuan", Symbol = "¥", IsActive = true },
            new() { Id = 11, Code = "CHF", Name = "Swiss Franc", Symbol = "CHF", IsActive = true },
            new() { Id = 12, Code = "ZAR", Name = "South African Rand", Symbol = "R", IsActive = true }
        };

            return isActive ? list.Where(x => x.IsActive).ToList() : list;
        }
    }
}
