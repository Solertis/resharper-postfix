﻿using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.DataContext;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Tips;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.PostfixTemplates.Contexts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.TextControl.Util;
using JetBrains.UI.ActionsRevised;
using JetBrains.UI.ActionSystem.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.PostfixTemplates
{
  [SolutionComponent]
  public sealed class PostfixTemplatesTabTracker
  {
    public PostfixTemplatesTabTracker(
      [NotNull] Lifetime lifetime, [NotNull] IActionManager manager, [NotNull] ICommandProcessor commandProcessor,
      [NotNull] ILookupWindowManager lookupWindowManager, [NotNull] PostfixTemplatesManager templatesManager,
      [NotNull] TextControlChangeUnitFactory changeUnitFactory)
    {
      // override live templates expand action
      var expandAction = manager.Defs.TryGetActionDefById(TextControlActions.TAB_ACTION_ID);
      if (expandAction != null)
      {
        var postfixHandler = new ExpandPostfixTemplateHandler(
          commandProcessor, lookupWindowManager, templatesManager, changeUnitFactory);

        lifetime.AddBracket(
          FOpening: () => manager.Handlers.AddHandler(expandAction, postfixHandler),
          FClosing: () => manager.Handlers.RemoveHandler(expandAction, postfixHandler));
      }
    }

    private sealed class ExpandPostfixTemplateHandler : IExecutableAction
    {
      [NotNull] private readonly ICommandProcessor myCommandProcessor;
      [NotNull] private readonly ILookupWindowManager myLookupWindowManager;
      [NotNull] private readonly PostfixTemplatesManager myTemplatesManager;
      [NotNull] private readonly TextControlChangeUnitFactory myChangeUnitFactory;

      public ExpandPostfixTemplateHandler([NotNull] ICommandProcessor commandProcessor,
                                          [NotNull] ILookupWindowManager lookupWindowManager,
                                          [NotNull] PostfixTemplatesManager templatesManager,
                                          [NotNull] TextControlChangeUnitFactory changeUnitFactory)
      {
        myChangeUnitFactory = changeUnitFactory;
        myLookupWindowManager = lookupWindowManager;
        myCommandProcessor = commandProcessor;
        myTemplatesManager = templatesManager;
      }

      public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
      {
        var textControl = context.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl == null) return nextUpdate();

        var solution = context.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        if (solution == null) return nextUpdate();

        var sourceFile = textControl.Document.GetPsiSourceFile(solution);
        if (sourceFile != null)
        {
          var template = GetTemplateFromTextControl(solution, textControl);
          if (template != null) return true;
        }

        return nextUpdate();
      }

      public void Execute(IDataContext context, DelegateExecute nextExecute)
      {
        var solution = context.GetData(ProjectModel.DataContext.DataConstants.SOLUTION);
        if (solution == null) return;

        var textControl = context.GetData(TextControl.DataContext.DataConstants.TEXT_CONTROL);
        if (textControl == null) return;

        if (myLookupWindowManager.CurrentLookup != null) return;

        const string commandName = "Expanding postfix template with [Tab]";
        var updateCookie = myChangeUnitFactory.CreateChangeUnit(textControl, commandName);
        try
        {
          using (myCommandProcessor.UsingCommand(commandName))
          {
            var postfixItem = GetTemplateFromTextControl(solution, textControl);
            if (postfixItem != null)
            {
              TipsManager.Instance.FeatureIsUsed(
                "Plugin.ControlFlow.PostfixTemplates.<tab>", textControl.Document, solution);

              var nameLength = postfixItem.Placement.OrderString.Length;
              var offset = textControl.Caret.Offset() - nameLength;

              postfixItem.Accept(
                textControl, TextRange.FromLength(offset, nameLength),
                LookupItemInsertType.Insert, Suffix.Empty, solution, keepCaretStill: false);

              return;
            }

            updateCookie.Dispose();
          }
        }
        catch
        {
          updateCookie.Dispose();
          throw;
        }

        nextExecute();
      }

      [CanBeNull]
      private ILookupItem GetTemplateFromTextControl([NotNull] ISolution solution, [NotNull] ITextControl textControl)
      {
        var offset = textControl.Caret.Offset();
        var prefix = LiveTemplatesManager.GetPrefix(textControl.Document, offset);

        if (!TemplateWithNameExists(prefix)) return null;

        // todo: this reparse is language-specific
        var postfixItems = TryReparseWith(solution, textControl, prefix, "__")
                        ?? TryReparseWith(solution, textControl, prefix, "__;");

        if (postfixItems == null) return null;
        if (postfixItems.Count != 1) return null;

        return postfixItems[0];
      }

      [CanBeNull]
      private IList<ILookupItem> TryReparseWith([NotNull] ISolution solution, [NotNull] ITextControl textControl,
                                                [NotNull] string templateName, [NotNull] string reparseString)
      {
        var offset = textControl.Caret.Offset();
        var document = textControl.Document;

        try
        {
          document.InsertText(offset, reparseString);
          solution.GetPsiServices().CommitAllDocuments();

          var executionContext = new PostfixExecutionContext(solution, textControl, reparseString, false);

          foreach (var position in TextControlToPsi.GetElements<ITokenNode>(solution, document, offset))
          {
            var postfixContext = myTemplatesManager.IsAvailable(position, executionContext);
            if (postfixContext == null) continue;

            return myTemplatesManager.CollectItems(postfixContext, templateName: templateName);
          }

          return null;
        }
        finally
        {
          var reparseRange = TextRange.FromLength(offset, reparseString.Length);
          document.DeleteText(reparseRange);
        }
      }

      private bool TemplateWithNameExists([NotNull] string prefix)
      {
        foreach (var providerInfo in myTemplatesManager.TemplateProvidersInfos)
        {
          if (providerInfo.Metadata.TemplateName == prefix) return true;
        }

        return false;
      }
    }
  }
}