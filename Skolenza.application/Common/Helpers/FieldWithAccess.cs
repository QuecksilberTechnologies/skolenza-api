using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
        public class FieldWithAccess<T>
    {
        public T Value { get; set; }
        public bool IsReadOnly { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public FieldWithAccess() { }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public FieldWithAccess(T value, bool isReadOnly)
        {
            Value = value;
            IsReadOnly = isReadOnly;
        }
    }

}
