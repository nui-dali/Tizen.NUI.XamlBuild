using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Tizen.NUI.EXaml
{
    internal class EXamlDefinitionList<T> : IEnumerable<(TypeReference, T)> where T : IMemberDefinition
    {
        internal void Clear()
        {
            List.Clear();
        }

        internal void Add(TypeReference declareTypeRef, T definition)
        {
            if (-1 == GetIndex(declareTypeRef, definition))
            {
                List.Add((declareTypeRef, definition));
            }
        }

        internal int GetIndex(TypeReference declareTypeRef, T definition)
        {
            for (int i = 0; i < List.Count; i++)
            {
                if (EXamlUtility.IsSameTypeReference(declareTypeRef, List[i].Item1)
                    &&
                    definition.Equals(List[i].Item2))
                {
                    return i;
                }
            }

            return -1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator<(TypeReference, T)> IEnumerable<(TypeReference, T)>.GetEnumerator()
        {
            return List.GetEnumerator();
        }

        private List<(TypeReference, T)> List = new List<(TypeReference, T)>();
    }
}
