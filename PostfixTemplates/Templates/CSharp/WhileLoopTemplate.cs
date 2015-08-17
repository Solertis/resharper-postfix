﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Contexts.CSharp;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.PostfixTemplates.Templates.CSharp
{
  [PostfixTemplate(
    templateName: "while",
    description: "Iterating while boolean statement is 'true'",
    example: "while (expr)")]
  public sealed class WhileLoopTemplate : BooleanExpressionTemplateBase, IPostfixTemplate
  {
    protected override ILookupItem CreateBooleanItem(CSharpPostfixExpressionContext expression)
    {
      if (expression.CanBeStatement)
      {
        return new WhileItem(expression);
      }

      return null;
    }

    private sealed class WhileItem : StatementPostfixLookupItem<IWhileStatement>
    {
      public WhileItem([NotNull] CSharpPostfixExpressionContext context) : base("while", context) { }

      protected override IWhileStatement CreateStatement(
        CSharpElementFactory factory, ICSharpExpression expression)
      {
        var template = "while($0)" + EmbeddedStatementBracesTemplate;
        return (IWhileStatement) factory.CreateStatement(template, expression);
      }
    }
  }
}