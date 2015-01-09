﻿using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  [SettingsKey(typeof(EnvironmentSettings), "Postfix templates settings")]
  public sealed class PostfixTemplatesSettings
  {
    [SettingsIndexedEntry("Template providers list disabled/enabled list")]
    public IIndexedEntry<string, bool> DisabledProviders;

    [SettingsEntry(true, "Show postfix templates in code completion")]
    public bool ShowPostfixItemsInCodeCompletion;

    [SettingsEntry(true, "Show static methods as instance members in code completion")]
    public bool ShowStaticMethodsInCodeCompletion;

    [SettingsEntry(true, "Show enumeration types helpers in code completion")]
    public bool ShowEnumHelpersInCodeCompletion;

    [SettingsEntry(true, "Alias .Count property as .Length in code completion and vice versa")]
    public bool ShowLengthCountItemsInCodeCompletion;

    [SettingsEntry(true, "Insert braces for embedded statements")]
    public bool UseBracesForEmbeddedStatements;

    [SettingsEntry(false, "Invoke parameter info from templates")]
    public bool InvokeParameterInfoFromTemplates;

    [SettingsEntry(true, "Search for occurrences in .var template")]
    public bool SearchOccurrencesFromIntroduceVarTemplates;
  }

  public static class PostfixSettingsAccessor
  {
    [NotNull] public static readonly Expression<Func<PostfixTemplatesSettings, IIndexedEntry<string, bool>>>
      DisabledProviders = x => x.DisabledProviders;
    [NotNull] public static readonly Expression<Func<PostfixTemplatesSettings, bool>>
      ShowPostfixItems = x => x.ShowPostfixItemsInCodeCompletion,
      ShowStaticMethods = x => x.ShowStaticMethodsInCodeCompletion,
      ShowEnumHelpers = x => x.ShowEnumHelpersInCodeCompletion,
      ShowLengthCountItems = x => x.ShowLengthCountItemsInCodeCompletion,
      BracesForStatements = x => x.UseBracesForEmbeddedStatements,
      InvokeParameterInfo = x => x.InvokeParameterInfoFromTemplates,
      SearchVarOccurrences = x => x.SearchOccurrencesFromIntroduceVarTemplates;
  }
}