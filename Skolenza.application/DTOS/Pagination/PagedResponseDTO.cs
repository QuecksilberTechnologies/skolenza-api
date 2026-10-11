using skolenza.application.DTOS.Staff.BaseStaffMember;
using System;
using System.Collections.Generic;

namespace skolenza.application.DTOS.Pagination
{
    public class CountingData
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InActiveCount { get; set; }
        public int OnLeaveCount { get; set; }
    }
    public class PagedResponseDTO<T>
    {
        public PagedResponseDTO()
        {
            Data = new List<T>();
        }

        public PagedResponseDTO(
            List<T> items,
            int totalCount,
            int pageNumber,
            int pageSize,
            bool? hasUploadedAll = null,
            double? completionPercentage = null)
            {

            Data = items ?? new List<T>();
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize > 0 ? pageSize : 10; // fallback if invalid
            HasUploadedAll = hasUploadedAll;
            CompletionPercentage = completionPercentage;
            IsPrimaryMarked = null;
           
            }
     
        public List<T> Data { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }

        private int _pageSize;
        public int PageSize
        {
            get => _pageSize <= 0 ? 10 : _pageSize;
            set => _pageSize = value;
        }

        // ❌ existing logic maintain kiya gaya
        public int TotalPages = 1;

        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;

        // ✅ Nullable dynamic fields (additions)
        public bool? HasUploadedAll { get; set; }
        public bool? IsPrimaryMarked { get; set; }
        public double? CompletionPercentage { get; set; }
        
    }

}
