using System;
using System.Collections.Generic;
using System.Text;

namespace Php
{
    class InjectionPoint
    {
        public String Url;
        public Boolean Isdealed=false;
        public Boolean CanInject=false;
        public Boolean IsSensitive = false;
        public Boolean CanConnect = true;

        public InjectionPoint(String url, Boolean isdealed, Boolean caninject, Boolean IsSensitive)
        {
            this.Url = url;
            this.Isdealed = isdealed;
            this.CanInject = caninject;
            this.IsSensitive = IsSensitive;
        }
    }
}
