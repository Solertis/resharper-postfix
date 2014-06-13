﻿using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ReSharper.PostfixTemplates;
using JetBrains.Threading;
using NUnit.Framework;
#if RESHARPER8
using JetBrains.Util;
#elif RESHARPER9
using JetBrains.TestFramework;
using JetBrains.ReSharper.Resources.Shell;
#pragma warning disable 618
#endif

[assembly: TestDataPathBase(@".\Data\Completion")]

// ReSharper disable once CheckNamespace
[SetUpFixture]
public class PostfixTestsAssembly : ReSharperTestEnvironmentAssembly
{
  [NotNull]
  private static IEnumerable<Assembly> GetAssembliesToLoad()
  {
    yield return typeof(PostfixTemplatesManager).Assembly;
    yield return Assembly.GetExecutingAssembly();
  }

  public override void SetUp()
  {
    base.SetUp();
    ReentrancyGuard.Current.Execute("LoadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.LoadAssemblies(GetType().Name, GetAssembliesToLoad());
    });
  }

  public override void TearDown()
  {
    ReentrancyGuard.Current.Execute("UnloadAssemblies", () => {
      var assemblyManager = Shell.Instance.GetComponent<AssemblyManager>();
      assemblyManager.UnloadAssemblies(GetType().Name, GetAssembliesToLoad());
    });

    base.TearDown();
  }
}