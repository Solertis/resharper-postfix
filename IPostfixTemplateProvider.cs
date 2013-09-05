﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Lookup;

namespace JetBrains.ReSharper.ControlFlow.PostfixCompletion
{
  public interface IPostfixTemplateProvider
  {
    void CreateItems([NotNull] PostfixTemplateAcceptanceContext context,
                     [NotNull] ICollection<ILookupItem> consumer);
  }

  [AttributeUsage(AttributeTargets.Class), MeansImplicitUse]
  [BaseTypeRequired(typeof(IPostfixTemplateProvider))]
  public sealed class PostfixTemplateProviderAttribute : ShellComponentAttribute
  {
    [NotNull] public string[] TemplateNames { get; private set; }
    [NotNull] public string Description { get; private set; }
    [NotNull] public string Example { get; private set; }

    public bool WorksOnTypes { get; set; }
    public bool DisabledByDefault { get; set; }

    public PostfixTemplateProviderAttribute(
      [NotNull] string templateName, [NotNull] string description, string example = null)
    {
      TemplateNames = new[] { templateName };
      Description = description;
      Example = example ?? string.Empty;
    }

    public PostfixTemplateProviderAttribute(
      [NotNull] string[] templateNames, [NotNull] string description, string example = null)
    {
      TemplateNames = templateNames;
      Description = description;
      Example = example ?? string.Empty;
    }
  }
}