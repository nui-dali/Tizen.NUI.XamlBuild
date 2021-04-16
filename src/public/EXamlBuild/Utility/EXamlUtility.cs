using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tizen.NUI.EXaml
{
    internal class EXamlUtility
    {
        internal static bool IsSameTypeReference(TypeReference typeReference1, TypeReference typeReference2)
        {
            if (null == typeReference1 || null == typeReference2)
            {
                return typeReference1 == typeReference2;
            }
            else if (typeReference1.Resolve() != typeReference2.Resolve())
            {
                return false;
            }
            else if (typeReference1 is GenericInstanceType)
            {
                if (typeReference2 is GenericInstanceType)
                {
                    var gType1 = typeReference1 as GenericInstanceType;
                    var gType2 = typeReference2 as GenericInstanceType;

                    if (gType1.GenericArguments.Count != gType2.GenericArguments.Count)
                    {
                        return false;
                    }
                    else
                    {
                        for (int i = 0; i < gType1.GenericArguments.Count; i++)
                        {
                            if (false == IsSameTypeReference(gType1.GenericArguments[i], gType2.GenericArguments[i]))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            else
            {
                return true;
            }
        }

    }
}
