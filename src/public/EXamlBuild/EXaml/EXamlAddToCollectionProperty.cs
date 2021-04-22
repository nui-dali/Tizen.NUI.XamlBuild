using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Tizen.NUI.Binding;

namespace Tizen.NUI.EXaml
{
    //use cc
    internal class EXamlAddToCollectionProperty : EXamlOperation
    {
        internal override string Write()
        {
            string ret = "";
            return ret;
        }

        public EXamlAddToCollectionProperty(EXamlCreateObject @object, string propertyName, object[] value)
        {
            EXamlOperation.eXamlOperations.Add(this);
        }
    }
}
