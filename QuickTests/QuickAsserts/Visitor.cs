using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace QuickAsserts
{
    public class Visitor : SymbolVisitor
    {
        public string ParentName { get; set; } = null;
        private static string currentParentName = null;
        private static string firstOrLast = null;
        public List<ReflectedProperty> Properties { get; set; } = new List<ReflectedProperty>();
        public override void VisitProperty(IPropertySymbol symbol)
        {
            var type = symbol.Type;

            var rp = new ReflectedProperty();

            if (type.Name == "List" || type.Name == "Dictionary")
            {
                rp.Name = firstOrLast != null
                    ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.Count"
                    : $"{currentParentName}.{symbol.Name}.Count";
                rp.Type = "Int32";
                Properties.Add(rp);
            }
            else if (type.BaseType.Name == "Array")
            {
                rp.Name = firstOrLast != null
                    ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.Count()"
                    : $"{currentParentName}.{symbol.Name}.Count()";
                rp.Type = "Int32";
                Properties.Add(rp);
            }
            else if (type.BaseType.Name == "Enum")
            {
                rp.Name = firstOrLast != null
                    ? $"{currentParentName}.{firstOrLast}.{symbol.Name}"
                    : $"{currentParentName}.{symbol.Name}";
                rp.Type = type.BaseType.Name;
                Properties.Add(rp);
            }
            else if(!IsCustomType(type.Name))
            {
                rp.Name = firstOrLast != null
                    ? $"{currentParentName}.{firstOrLast}.{symbol.Name}"
                    : $"{currentParentName}.{symbol.Name}";
                rp.Type = type.Name;
                Properties.Add(rp);
            }
            

            

            if (type.Name == "List")
            {
                type = ((INamedTypeSymbol)symbol.GetMethod.ReturnType).TypeArguments[0];
                if (IsCustomType(type.Name))
                {
                    currentParentName = $"{currentParentName}.{symbol.Name}";
                    
                    firstOrLast = "FirstOrDefault()";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = "LastOrDefault()";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = null;

                    currentParentName = ParentName;
                    base.VisitProperty(symbol);
                }
                else
                {
                    type = ((INamedTypeSymbol)symbol.GetMethod.ReturnType).TypeArguments[0];
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.FirstOrDefault()"
                        : $"{currentParentName}.{symbol.Name}.FirstOrDefault()";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.LastOrDefault()"
                        : $"{currentParentName}.{symbol.Name}.LastOrDefault()";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                }
            }
            else if (type.BaseType.Name == "Array")
            {
                type = ((IArrayTypeSymbol)symbol.GetMethod.ReturnType).ElementType;
                if (IsCustomType(type.Name))
                {
                    currentParentName = $"{currentParentName}.{symbol.Name}";
                    
                    firstOrLast = "FirstOrDefault()";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = "LastOrDefault()";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = null;

                    currentParentName = ParentName;
                    base.VisitProperty(symbol);
                }
                else
                {
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.FirstOrDefault()"
                        : $"{currentParentName}.{symbol.Name}.FirstOrDefault()";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.LastOrDefault()"
                        : $"{currentParentName}.{symbol.Name}.LastOrDefault()";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                }
            }
            else if (type.Name == "Dictionary")
            {
                type = ((INamedTypeSymbol)symbol.GetMethod.ReturnType).TypeArguments[1];
                if (IsCustomType(((INamedTypeSymbol) symbol.GetMethod.ReturnType).TypeArguments[1].Name))
                {
                    currentParentName = $"{currentParentName}.{symbol.Name}";

                    firstOrLast = "FirstOrDefault().Value";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = "LastOrDefault().Value";
                    VisitNamedType(type as INamedTypeSymbol);
                    firstOrLast = null;

                    currentParentName = ParentName;
                    base.VisitProperty(symbol);
                }
                else
                {
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.FirstOrDefault().Value"
                        : $"{currentParentName}.{symbol.Name}.FirstOrDefault().Value";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                    rp = new ReflectedProperty();
                    rp.Name = firstOrLast != null
                        ? $"{currentParentName}.{firstOrLast}.{symbol.Name}.LastOrDefault().Value"
                        : $"{currentParentName}.{symbol.Name}.LastOrDefault().Value";
                    rp.Type = type.Name;
                    Properties.Add(rp);
                }
            }
            else if (type.BaseType.Name == "Object" && IsCustomType(type.Name))
            {
                currentParentName = $"{currentParentName}.{symbol.Name}";

                VisitNamedType(type as INamedTypeSymbol);

                currentParentName = ParentName;
                base.VisitProperty(symbol);
            }
        }

        //NOTE: We have to visit the named type's children even though
        //we don't care about them. :(
        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (currentParentName == null)
                currentParentName = ParentName;

            foreach (var child in symbol.GetMembers().Where(x => x.DeclaredAccessibility == Accessibility.Public))
            {
                child.Accept(this);
            }
        }

        private static bool IsCustomType(string typeName)
        {
            return
                typeName != "String" &&
                typeName != "Int16" &&
                typeName != "Int32" &&
                typeName != "Int64" &&
                typeName != "DateTime";
        }
    }
}