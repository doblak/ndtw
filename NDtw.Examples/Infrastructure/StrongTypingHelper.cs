using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NDtw.Examples.Infrastructure
{
    public class StrongTypingHelper
    {
        //usage: GetPropertyAsString<MyType>(x => x.MyTypeProperty);
        public static string GetPropertyAsString<T>(Expression<Func<T, object>> expression)
        {
            return GetProperty(expression).Name;
        }

        public static PropertyInfo GetProperty<T>(Expression<Func<T>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member != null)
                return member.Member as PropertyInfo;
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                member = ((UnaryExpression)expression.Body).Operand as MemberExpression;
                if (member != null)
                    return member.Member as PropertyInfo;
            }

            throw new ArgumentException(@"Expression is not a member access", expression.ToString());
        }

        public static PropertyInfo GetProperty<T, TR>(Expression<Func<T, TR>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member != null)
                return (PropertyInfo)member.Member;
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                member = ((UnaryExpression)expression.Body).Operand as MemberExpression;
                if (member != null)
                    return (PropertyInfo)member.Member;
            }

            throw new ArgumentException(@"Expression is not a member access", expression.ToString());
        }
    }
}
