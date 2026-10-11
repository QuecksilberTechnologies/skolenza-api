using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public static class ExperienceLogicHelper
    {
        public static string GetExperienceCase(bool isFresher, bool isGap, bool isInfoLatestYear)
        {
            if (isFresher && isGap)
                return "FRESHER_WITH_GAP";

            if (isFresher && !isGap)
                return "FRESHER_NO_GAP";

            if (!isFresher && isGap && isInfoLatestYear)
                return "EXPERIENCE_GAP_LATEST_YEAR";

            if (!isFresher && !isGap)
                return "EXPERIENCE_NO_GAP";

            return "INVALID";
        }
    }

}
