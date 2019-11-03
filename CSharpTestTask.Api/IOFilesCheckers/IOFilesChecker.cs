using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace CSharpTestTask.Api.IOFilesCheckers
{
    public class IOFilesChecker : IIOFilesChecker
    {
        private readonly string _inputFileName;
        private readonly string _outputFileName;

        public IOFilesChecker(string inputFileName, string outputFileName)
        {
            _inputFileName = inputFileName;
            _outputFileName = outputFileName;
        }
        public (string message, bool success) CheckFiles()
        {
            try
            {
                using var fileStream = new FileStream(_inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                return ($"Error: input file '{_inputFileName}' was not found", false);
            }

            catch (UnauthorizedAccessException)
            {
                return ($"Error: input file access permission denied", false);
            }
            catch (SecurityException)
            {
                return ($"Error: input file access permission denied", false);
            }
            catch (PathTooLongException)
            {
                return ($"Error: path is too long", false);
            }
            catch (IOException)
            {
                return ($"Error: The input filename, directory name, or volume label syntax is incorrect", false);
            }

            try
            {
                using var fileStream = new FileStream(_outputFileName, FileMode.Create, FileAccess.Write, FileShare.Write);
            }

            catch (UnauthorizedAccessException)
            {
                return ($"Error: output file access permission denied", false);
            }
            catch (SecurityException)
            {
                return ($"Error: output file access permission denied", false);
            }
            catch (PathTooLongException)
            {
                return ($"Error: output is too long", false);
            }
            catch (IOException)
            {
                return ($"Error: output filename, directory name, or volume label syntax is incorrect", false);
            }

            return ("Input and output files are ok", true);
        }
    }
}
