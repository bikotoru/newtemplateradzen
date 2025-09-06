using System.Linq.Expressions;

namespace Shared.Models.Extensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Obtiene el nombre de la propiedad de una expression
        /// </summary>
        public static string GetPropertyName<T>(this Expression<Func<T, object>> expression)
        {
            return GetPropertyName(expression.Body);
        }

        /// <summary>
        /// Obtiene el nombre de la propiedad de una expression genérica
        /// </summary>
        public static string GetPropertyName(this Expression expression)
        {
            return expression switch
            {
                MemberExpression memberExpression => memberExpression.Member.Name,
                UnaryExpression { Operand: MemberExpression memberExpr } => memberExpr.Member.Name,
                _ => throw new ArgumentException($"Expression '{expression}' is not a valid property expression.")
            };
        }

        /// <summary>
        /// Obtiene los nombres de las propiedades de una lista de expressions
        /// </summary>
        public static string[] GetPropertyNames<T>(this Expression<Func<T, object>>[] expressions)
        {
            return expressions.Select(expr => expr.GetPropertyName()).ToArray();
        }

        /// <summary>
        /// Obtiene el path completo de la propiedad (para propiedades anidadas)
        /// </summary>
        public static string GetPropertyPath<T>(this Expression<Func<T, object>> expression)
        {
            return GetPropertyPath(expression.Body);
        }

        private static string GetPropertyPath(Expression expression)
        {
            return expression switch
            {
                MemberExpression memberExpression => 
                    memberExpression.Expression != null && memberExpression.Expression.NodeType == ExpressionType.MemberAccess
                        ? $"{GetPropertyPath(memberExpression.Expression)}.{memberExpression.Member.Name}"
                        : memberExpression.Member.Name,
                UnaryExpression { Operand: MemberExpression memberExpr } => GetPropertyPath(memberExpr),
                _ => throw new ArgumentException($"Expression '{expression}' is not a valid property expression.")
            };
        }
    }
}