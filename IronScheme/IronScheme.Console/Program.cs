#region License
/* Copyright (c) 2007-2016 Llewellyn Pritchard
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See docs/license.txt. */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using IronScheme.Hosting;
using IronScheme.Runtime;

namespace IronScheme.Console
{
  class Program
  {
    static string[] PrefixArgs(string[] args, params string[] extra)
    {
      var newargs = new string[args.Length + extra.Length];
      Array.Copy(extra, newargs, extra.Length);
      Array.Copy(args, 0, newargs, extra.Length, args.Length);
      return newargs;
    }

    static int Main(string[] args)
    {
#if !NETCOREAPP2_1_OR_GREATER
      if ((Array.IndexOf(args, "-profile")) >= 0)
      {
        const string PROFILER_GUID = "{9E2B38F2-7355-4C61-A54F-434B7AC266C0}";

        if (ExecuteRegsvr32(false, true))
        {
          ProcessStartInfo psi = new ProcessStartInfo(typeof(Program).Assembly.Location);
          psi.EnvironmentVariables["COR_PROFILER"] = PROFILER_GUID;
          psi.EnvironmentVariables["COR_ENABLE_PROFILING"] = "1";

          // ----- RUN THE PROCESS -------------------
          args = Array.FindAll(args, x => x != "-profile");

          psi.Arguments = string.Join(" ", args);
          psi.UseShellExecute = false;

          try
          {
            Process p = Process.Start(psi);

            p.WaitForExit();

            return p.ExitCode;
          }
          finally
          {
            ExecuteRegsvr32(false, false);
          }
        }
        else
        {
          // remove -profile
          args = Array.FindAll(args, x => x != "-profile");
        }
      }
#endif
      var exename = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

      switch (exename)
      {
        case "isc":
        case "isc32":
        case "ironscheme":
          args = PrefixArgs(args, "-nologo");
          break;
      }

      args = Args(args, "--show-loaded-libraries", () => Builtins.ShowImports = true);
      args = ParseIncludes(args);

      Encoding oo = System.Console.OutputEncoding;
#if !NETCOREAPP2_1_OR_GREATER
      EnableMulticoreJIT();
#endif
      try
      {
        System.Console.OutputEncoding = Encoding.UTF8;
#if NETCOREAPP2_1_OR_GREATER
        Builtins.Exact(1);
#endif

        return IronSchemeConsoleHost.Execute(args);
      }
      finally
      {
        System.Console.OutputEncoding = oo;
      }
    }

    static string[] ParseIncludes(string[] args)
    {
      var aa = new List<string>();

      for (int i = 0; i < args.Length; i++)
      {
        var a = args[i];
        if (a == "-I")
        {
          i++;
          if (i < args.Length)
          {
            var p = args[i];
            Builtins.AddIncludePath(p);
          }
          else
          {
            System.Console.Error.WriteLine("Error: Missing include path");
            Environment.Exit(1);
          }
        }
        else
        {
          aa.Add(a);
        }
      }

      return aa.ToArray();
    }

    // no System.Core here...
    delegate void Action();

    static string[] Args(string[] args, string argname, Action handler)
    {
      var i = Array.IndexOf(args, argname);
      if (i >= 0) // why are we still fuckin writing code like this?
      {
        args = Array.FindAll(args, x => x != argname);
        handler();
      }
      return args;
    }

#if !NETCOREAPP2_1_OR_GREATER

    static void EnableMulticoreJIT()
    {
      var type = typeof(object).Assembly.GetType("System.Runtime.ProfileOptimization", false);
      if (type != null)
      {
        var sprmi = type.GetMethod("SetProfileRoot");
        var spmi = type.GetMethod("StartProfile");
        if (sprmi != null && spmi != null)
        {
          Action<string> SetProfileRoot = Delegate.CreateDelegate(typeof(Action<string>), sprmi) as Action<string>;
          Action<string> SetProfile = Delegate.CreateDelegate(typeof(Action<string>), spmi) as Action<string>;

          var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IronScheme\\JITCache");

          Directory.CreateDirectory(dir);

          SetProfileRoot(dir);
          SetProfile("IronScheme");
        }
      }
    }

    // https://github.com/sawilde/opencover/blob/master/main/OpenCover.Framework/ProfilerRegistration.cs
    static bool ExecuteRegsvr32(bool userRegistration, bool register)
    {
      const string UserRegistrationString = "/n /i:user";

      var ppath = GetProfilerPath();

      if (!File.Exists(ppath))
      {
        if (register)
        {
          System.Console.WriteLine("WARNING: Profiler DLL ({0}) not available in application directory. Profiling will be disabled.", Path.GetFileName(ppath));
        }

        return false;
      }

      var startInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "regsvr32.exe"),
                               string.Format("/s {2} {0} \"{1}\"", userRegistration ? UserRegistrationString : String.Empty,
                               ppath, register ? string.Empty : "/u")) { CreateNoWindow = true };

      var process = Process.Start(startInfo);
      process.WaitForExit();
      return true;
    }

    static string GetProfilerPath()
    {
      var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
      return Path.Combine(dir, string.Format("IronScheme.Profiler.x{0}.dll", IntPtr.Size == 8 ? "64" : "86"));
    }
#endif
  }
}
