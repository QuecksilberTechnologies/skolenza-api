using skolenza.application.Common.Attributes;
using skolenza.application.DTOs.Staff.AccessControlReadOnlyType;
using skolenza.application.DTOs.Staff.AccessResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class StaffMembereEducationInfoMapperHelper
    {
        public static GetEducationAccessResponseDTO ConvertToAccessResponseDTO<T>(T source)
        {
            var result = new GetEducationAccessResponseDTO();
            var sourceProps = typeof(T).GetProperties();
            var targetProps = typeof(GetEducationAccessResponseDTO).GetProperties();

            foreach (var sourceProp in sourceProps)
            {
                try
                {
                    var targetProp = targetProps.FirstOrDefault(p =>
                        p.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase));

                    if (targetProp == null || !targetProp.CanWrite)
                        continue; // Skip if target property not found or readonly

                    var value = sourceProp.GetValue(source);

                    var sourceAttrProp = typeof(StaffMemberEducationEditableFieldsDTO)
                           .GetProperty(sourceProp.Name);
                    var accessAttr = sourceAttrProp?.GetCustomAttribute<AccessControlAttribute>();
                    bool isReadOnly = accessAttr?.ReadOnly ?? false;


                    if (!targetProp.PropertyType.IsGenericType ||
                        targetProp.PropertyType.GetGenericTypeDefinition() != typeof(FieldWithAccess<>))
                        continue;

                    var targetGenericType = targetProp.PropertyType.GetGenericArguments()[0];

                    // Safe value creation for value types
                    object? safeValue;
                    if (value == null && targetGenericType.IsValueType)
                        safeValue = Activator.CreateInstance(targetGenericType);
                    else
                        safeValue = value;

                    var fieldType = typeof(FieldWithAccess<>).MakeGenericType(targetGenericType);
                    var instance = Activator.CreateInstance(fieldType, safeValue, isReadOnly);

                    targetProp.SetValue(result, instance);
                }
                catch (Exception ex)
                {
                    // Log exact property causing issue
                    Console.WriteLine($"Mapping failed for {sourceProp.Name}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
