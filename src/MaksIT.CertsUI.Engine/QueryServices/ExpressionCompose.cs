using System.Linq.Expressions;

namespace MaksIT.CertsUI.Engine.QueryServices;

/// <summary>
/// Composes a predicate on a related entity into a predicate on the outer entity via a navigation expression.
/// Uses parameter replacement so the result is Linq2Db/IQueryable translatable to SQL.
/// </summary>
public static class ExpressionCompose {
  /// <summary>
  /// Composes inner predicate with navigation to produce a predicate on the outer type (Linq2Db/IQueryable translatable to SQL).
  /// </summary>
  public static Expression<Func<TOuter, bool>>? ComposeNavigationPredicate<TOuter, TInner>(
    Expression<Func<TInner, bool>>? innerPredicate,
    Expression<Func<TOuter, TInner>> navigation) {
    if (innerPredicate == null) return null;
    ArgumentNullException.ThrowIfNull(navigation);

    var visitor = new ReplaceParameterWithExpressionVisitor(innerPredicate.Parameters[0], navigation.Body);
    var newBody = visitor.Visit(innerPredicate.Body);
    return Expression.Lambda<Func<TOuter, bool>>(newBody, navigation.Parameters[0]);
  }

  private sealed class ReplaceParameterWithExpressionVisitor(ParameterExpression parameter, Expression replacement) : ExpressionVisitor {
    protected override Expression VisitParameter(ParameterExpression node) =>
      node == parameter ? replacement : base.VisitParameter(node);
  }
}
