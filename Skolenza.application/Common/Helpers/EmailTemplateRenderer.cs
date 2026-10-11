using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class EmailTemplateRenderer
    {
         
            public static string RenderBody(string template, Dictionary<string, string> placeholders)
            {
                foreach (var kvp in placeholders)
                {
                    // Support both {{Key}} and {Key} formats
                    template = template.Replace("{{" + kvp.Key + "}}", kvp.Value);
                    template = template.Replace("{" + kvp.Key + "}", kvp.Value);
                }
                return template;
            }
       

    }

}
