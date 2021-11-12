using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;

namespace BulkTakeFileOwner
{
    internal class Program
    {
        private const string OLDUSERSID = "S-1-5-21-648928976-2772963566-792071830-1001";
        private static string rootDir;
        private static string MYSID = UserPrincipal.Current.Sid.ToString();

        private static int changed = 0;
        private static int already = 0;
        private static int third = 0;
        private static void GetOwnerShip(string path)
        {
            FileSecurity fs = File.GetAccessControl(path);
            string sid = fs.GetOwner(typeof(SecurityIdentifier)).ToString();
            if (sid == OLDUSERSID)
            {
                fs.SetOwner(new NTAccount(Environment.UserDomainName, Environment.UserName));
                File.SetAccessControl(path, fs);
                Console.WriteLine($"Changed {path}");
                changed++;
            }
            else if (sid == MYSID)
            {
                already++;
            }
            else
            {
                third++;
                Console.WriteLine($"Skip {fs.GetOwner(typeof(NTAccount))}: {path}");
            }
        }
        
        private static IEnumerable TraverseDir(string root)
        {
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>(20);

            if (File.Exists(root))
            {
                yield return root;
                yield break;
            }
            if (!Directory.Exists(root))
            {
                Console.Error.WriteLine("Not a file or directory.");
                yield break;
            }
            
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                
                yield return currentDir;
                
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have
                // discovery permission on a folder or file. It may or may not be acceptable
                // to ignore the exception and continue enumerating the remaining files and
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception
                // will be raised. This will happen if currentDir has been deleted by
                // another application or thread after our call to Directory.Exists. The
                // choice of which exceptions to catch depends entirely on the specific task
                // you are intending to perform and also on how much you know with certainty
                // about the systems on which this code will run.
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                
                // Push the subdirectories onto the stack for traversal.
                foreach (string str in subDirs)
                    dirs.Push(str);
                
                string[] files;
                
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {

                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                // Perform the required action on each file here.
                // Modify this block to perform your required task.
                foreach (string file in files)
                {
                    yield return file;
                }
                
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            foreach (var root in args)
            {
                rootDir = root;
                foreach (string path in TraverseDir(rootDir))
                {
                    try
                    {
                        GetOwnerShip(path);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Error occured trying to process: \"{path}\"");
                        Console.Error.WriteLine(e);
                    }
                }
            }
            Console.WriteLine($"{changed} changed, {third} skipped, {already} already yours");
            Console.ReadLine();
        }
    }
}