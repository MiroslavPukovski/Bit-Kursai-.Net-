﻿using ManoPirmasDotNetProjektas.Paskaitos.EntityFramework;
using ManoPirmasDotNetProjektas.Paskaitos.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManoPirmasDotNetProjektas.Paskaitos.Linq
{
    public class LinqExecutor : ITema
    {
        private readonly string _baseDirectory = @"C:\Users\Emeil\source\repos\ManoPirmasDotNetProjektas\ManoPirmasDotNetProjektas\bin\Debug\net6.0";

        private readonly ILoggerServise _logger;
        private readonly BookStoreContext _dbContext;

        public LinqExecutor(ILoggerServise logger, BookStoreContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public Task Run()
        {
            ReadLargeFilesNoLinq();
            Console.WriteLine();
            ReadLargeFilesWithLinqQuerySyntax();
            Console.WriteLine();
            ReadLargeFilesWithLinqMethodSyntax();

            return Task.CompletedTask;
        }

        private void ReadLargeFilesNoLinq()
        {
            var dirInfo = new DirectoryInfo(_baseDirectory);
            var files = dirInfo.GetFiles();
            Array.Sort(files, (x, y) => y.Length.CompareTo(x.Length));

            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"{files[i].Name,-55} : {files[i].Length}");
            }
        }

        private void ReadLargeFilesWithLinqQuerySyntax()
        {
            var filesQ = from file in new DirectoryInfo(_baseDirectory).GetFiles()
                         orderby file.Length descending
                         select file;

            foreach (var file in filesQ.Take(10))
            {
                Console.WriteLine($"{file.Name,-55} : {file.Length}");
            }
        }

        private void ReadLargeFilesWithLinqMethodSyntax()
        {
            var filesQ = new DirectoryInfo(_baseDirectory).GetFiles().OrderByDescending(f => f.Length).Take(3);
           
            foreach (var file in filesQ)
            {
                Console.WriteLine($"{file.Name,-55} : {file.Length}");
            }
        }


    }
}
