﻿using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.LiveTemplates;
using JetBrains.ReSharper.PostfixTemplates.LookupItems;
using JetBrains.ReSharper.PostfixTemplates.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates.Templates
{
  [PostfixTemplate(
    templateName: "arg",
    description: "Surrounds expression with invocation",
    example: "Method(expr)")]
  public class ArgumentExpressionTemplate : IPostfixTemplate
  {
    public ILookupItem CreateItem(PostfixTemplateContext context)
    {
      if (context.IsAutoCompletion) return null;

      // disable .arg template if .arg hotspot is enabled now
      var textControl = context.ExecutionContext.TextControl;
      if (textControl.GetData(PostfixArgTemplateExpansion) != null) return null;

      var bestContext = CommonUtils.FindBestExpressionContext(context);
      if (bestContext == null) return null;

      return new ArgumentItem(bestContext);
    }

    [NotNull] private static readonly Key<object> PostfixArgTemplateExpansion =
      new Key(typeof(ArgumentExpressionTemplate).FullName);

    private class ArgumentItem : ExpressionPostfixLookupItem<IInvocationExpression>
    {
      [NotNull] private readonly ILookupItemsOwner myLookupItemsOwner;
      [NotNull] private readonly LiveTemplatesManager myTemplatesManager;

      public ArgumentItem([NotNull] PrefixExpressionContext context) : base("arg", context)
      {
        var executionContext = context.PostfixContext.ExecutionContext;
        myLookupItemsOwner = executionContext.LookupItemsOwner;
        myTemplatesManager = executionContext.LiveTemplatesManager;
      }

      protected override IInvocationExpression CreateExpression(CSharpElementFactory factory,
                                                                ICSharpExpression expression)
      {
        return (IInvocationExpression) factory.CreateExpression("Method($0)", expression);
      }

      protected override void AfterComplete(ITextControl textControl, IInvocationExpression expression)
      {
        var invocationRange = expression.InvokedExpression.GetDocumentRange();
        var languageType = expression.Language;
        var hotspotInfo = new HotspotInfo(new TemplateField("Method", 0), invocationRange);

        var argumentRange = expression.Arguments[0].Value.GetDocumentRange();
        var solution = expression.GetSolution();

        var marker = argumentRange.EndOffsetRange().CreateRangeMarker();
        var length = (marker.Range.EndOffset - invocationRange.TextRange.EndOffset);

        var session = myTemplatesManager.CreateHotspotSessionAtopExistingText(
          expression.GetSolution(), TextRange.InvalidRange, textControl,
          LiveTemplatesManager.EscapeAction.RestoreToOriginalText, hotspotInfo);

        var settings = expression.GetSettingsStore();
        var invokeParameterInfo = settings.GetValue(PostfixSettingsAccessor.InvokeParameterInfo);

        textControl.PutData(PostfixArgTemplateExpansion, string.Empty);

        session.Closed.Advise(Lifetime, _ =>
        {
          textControl.PutData(PostfixArgTemplateExpansion, null);

          using (ReadLockCookie.Create())
          {
            var hotspotRange = session.Hotspots[0].RangeMarker.Range;
            if (!hotspotRange.IsValid) return;

            solution.GetPsiServices().CommitAllDocuments();

            if (TryPlaceCaretSmart(solution, textControl, languageType, hotspotRange))
              return;

            var endOffset = hotspotRange.EndOffset + length;
            textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible);

            if (invokeParameterInfo)
            {
              var argumentsRange = TextRange.FromLength(hotspotRange.EndOffset, length + 1);
              LookupUtil.ShowParameterInfo(
                solution, textControl, argumentsRange, null, myLookupItemsOwner);
            }
          }
        });

        session.Execute();
      }

      private static bool TryPlaceCaretSmart([NotNull] ISolution solution, [NotNull] ITextControl textControl,
                                             [NotNull] PsiLanguageType language, TextRange range)
      {
        foreach (var tokenNode in
          TextControlToPsi.GetElements<ITokenNode>(solution, textControl.Document, range.EndOffset))
        {
          if (!tokenNode.Language.IsLanguage(language)) continue;
          if (tokenNode.GetTokenType() != CSharpTokenType.LPARENTH) continue;

          var invocation = tokenNode.Parent as IInvocationExpression;
          if (invocation == null || invocation.RPar == null) continue;

          var reference = invocation.InvocationExpressionReference;
          if (reference != null && reference.Resolve().ResolveErrorType == ResolveErrorType.OK)
          {
            var offset = invocation.RPar.GetDocumentRange().TextRange.EndOffset;
            textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible);
            return true;
          }
        }

        return false;
      }
    }
  }
}