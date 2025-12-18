using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using System.Net;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace FDCS.CRM.Backend.Infrastructure.Security
{
    public class InputSanitizationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Exclusion Logic: Skip TasksController
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                if (controllerActionDescriptor.ControllerName == "Tasks")
                {
                    return; // Skip sanitization for Tasks module
                }
            }

            // 2. Iterate over Action Arguments
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                // 3. Sanitize String Properties using Reflection
                SanitizeObject(argument);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do nothing after execution
        }

        private void SanitizeObject(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();

            // Handle Strings directly (though unlikely to be a top-level arg usually, but possible)
            if (type == typeof(string)) return; // Can't modify string by ref here easily in this loop structure

            // Iterate properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

            foreach (var property in properties)
            {
                var value = (string?)property.GetValue(obj);
                if (!string.IsNullOrEmpty(value))
                {
                    // HTML Encode
                    var sanitized = WebUtility.HtmlEncode(value);
                    property.SetValue(obj, sanitized);
                }
            }
        }
    }
}
