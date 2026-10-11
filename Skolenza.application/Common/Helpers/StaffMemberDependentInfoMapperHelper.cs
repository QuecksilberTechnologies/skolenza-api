using skolenza.application.Common.Attributes;
using skolenza.application.DTOs.Staff.AccessResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class StaffMemberDependentInfoMapperHelper
    {
        public static GetDependenAccessResponseDTO ConvertToAccessResponseDTO<T>(T source)
        {
            var result = new GetDependenAccessResponseDTO();

            var sourceProps = typeof(T).GetProperties();
            var targetProps = typeof(GetDependenAccessResponseDTO).GetProperties();

            foreach (var sourceProp in sourceProps)
            {
                try
                {
                    var targetProp = targetProps.FirstOrDefault(p => p.Name.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase));
                    if (targetProp == null) continue;

                    var value = sourceProp.GetValue(source);

                    var accessAttr = sourceProp.GetCustomAttributes(typeof(AccessControlAttribute), true)
                                               .FirstOrDefault() as AccessControlAttribute;

                    bool isReadOnly = accessAttr?.ReadOnly ?? false;

                    if (!targetProp.PropertyType.IsGenericType ||
                        targetProp.PropertyType.GetGenericTypeDefinition() != typeof(FieldWithAccess<>))
                        continue;

                    var targetGenericType = targetProp.PropertyType.GetGenericArguments()[0];

                    // ✅ Handle null value for value types
                    object? safeValue = value;
                    if (safeValue == null)
                    {
                        if (targetGenericType.IsValueType && Nullable.GetUnderlyingType(targetGenericType) == null)
                            safeValue = Activator.CreateInstance(targetGenericType); // default(int), etc.
                    }
                    else
                    {
                        try
                        {
                            var underlyingTargetType = Nullable.GetUnderlyingType(targetGenericType) ?? targetGenericType;

                            if (!underlyingTargetType.IsAssignableFrom(safeValue.GetType()))
                            {
                                safeValue = Convert.ChangeType(safeValue, underlyingTargetType);
                            }
                        }
                        catch (Exception convertEx)
                        {
                            Console.WriteLine($"⚠️ Conversion failed for property {sourceProp.Name}: {convertEx.Message}");
                            continue; // Skip if can't convert
                        }
                    }

                    var fieldType = typeof(FieldWithAccess<>).MakeGenericType(targetGenericType);
                    var constructor = fieldType.GetConstructor(new[] { targetGenericType, typeof(bool) });
                    if (constructor == null)
                    {
                        Console.WriteLine($"Constructor not found for property: {sourceProp.Name}");
                        continue;
                    }

                    var instance = constructor.Invoke(new[] { safeValue!, isReadOnly });
                    targetProp.SetValue(result, instance);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error mapping property '{sourceProp.Name}': {ex.Message}");
                }
            }

            return result;
        }

    }
}
