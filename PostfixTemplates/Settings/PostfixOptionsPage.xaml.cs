﻿using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Features.Intellisense.Options;
using JetBrains.UI.CrossFramework;
using JetBrains.UI.Options;

namespace JetBrains.ReSharper.PostfixTemplates.Settings
{
  [OptionsPage(
    id: PID, name: "Postfix Templates",
    typeofIcon: typeof(ServicesThemedIcons.SurroundTemplate),
    ParentId = IntelliSensePage.PID)]
  public sealed partial class PostfixOptionsPage : IOptionsPage
  {
    // ReSharper disable once InconsistentNaming
    private const string PID = "PostfixTemplates";

    public PostfixOptionsPage([NotNull] Lifetime lifetime,
      [NotNull] OptionsSettingsSmartContext store,
      [NotNull] PostfixTemplatesManager templatesManager)
    {
      InitializeComponent();
      DataContext = new PostfixOptionsViewModel(lifetime, store, templatesManager);
      Control = this;
    }

    public EitherControl Control { get; private set; }
    public string Id { get { return PID; } }
    public bool OnOk() { return true; }
    public bool ValidatePage() { return true; }

    private void DoubleClickCheck(object sender, RoutedEventArgs e)
    {
      var viewModel = ((FrameworkElement) sender).DataContext as PostfixTemplateViewModel;
      if (viewModel != null)
      {
        viewModel.IsChecked = !viewModel.IsChecked;
      }
    }

    private void SpaceBarCheck(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Space) return;

      var viewModel = ((FrameworkElement)sender).DataContext as PostfixTemplateViewModel;
      if (viewModel != null)
      {
        viewModel.IsChecked = !viewModel.IsChecked;
      }
    }
  }
}