using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace DistributedSearch
{
    class DistributedSearch
    {
        static class Global
        {
            //We use a global class so all threads could access it during run time
            public static String StringToSearch; // The string we search for
            public static int Delta; //The amount to jump between chars
            public static AutoResetEvent waitHandle = new AutoResetEvent(false); //waitHandle event
            public static CancellationTokenSource _tokenSource; //cancelTokenSource source
            public static CancellationToken cancelToken; //cancelToken token
            public static List<int> indexList;
            public static Mutex mut = new Mutex();
        }

        public static void search(Object state)
        {
            //We run a check to see if other thread found the string before commiting to proceed with search
            if (Global.cancelToken.IsCancellationRequested)
                return;

            object[] array = state as object[];
            StreamReader streamReader = ((StreamReader)(array[0]));
            int blockIndex = Convert.ToInt32(array[1]);
            //We run a check to see if other thread found the string before commiting to proceed with search
            if (Global.cancelToken.IsCancellationRequested)
                return;

            //We make a new buffer with size of 10k, and read a block into it from the stream reader
            char[] buffer = new char[10000];
            ((StreamReader)streamReader).ReadBlock(buffer, 0, buffer.Length);
            //After finishing to read, we release the wait so the next thread may continue to read without conflicts
            Global.waitHandle.Set();

            //We run a check to see if other thread found the string before commiting to proceed with search
            if (Global.cancelToken.IsCancellationRequested)
                return;

            //Now we check the buffer for the searched string
            for (int i = 0; i < buffer.Length; i++)
            {
                //When we find the first letter of our string
                if (buffer[i] == Global.StringToSearch[0])
                {
                    int place = 0;

                    //Check if there is enough place for the word to be in the current buffer
                    if (buffer.Length - i < Global.StringToSearch.Length)
                    {
                        break;
                    }

                    //Start going across with Delta jumps, checking if both chars in the buffer and our string match
                    for (int j = i; j < (i + (Global.StringToSearch.Length*(Global.Delta+1))) ; j += (Global.Delta + 1))
                    {
                        //If we find unmatching chars, we didn't find our string, keep searching
                        if (buffer[j] != Global.StringToSearch[place])
                        {
                            break;
                        }
                        //If we reached the end of the size of the string and all chars were matching, we found it
                        if (place == Global.StringToSearch.Length - 1 && buffer[j] == Global.StringToSearch[place])
                        {
                            //Use mutex to make sure only 1 thread can write to the list at a time to not lose any data
                            (Global.mut).WaitOne();
                            Global.indexList.Add((blockIndex*10000)+i); //update the index where we found the string
                            (Global.mut).ReleaseMutex();

                            Global._tokenSource.Cancel(); // make a cancel request since we finished our search
                            Global.waitHandle.Set(); //Wake up the main thread so we can stop the pool search
                            break;
                        }
                        place = place + 1;
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            //We define a cancelation token to know when to stop the search
            Global._tokenSource = new CancellationTokenSource();
            Global.cancelToken = Global._tokenSource.Token;

            //The first argument is the text file path
            String textPath = args[0];
            //The second argument is the string we want to search
            Global.StringToSearch = args[1];
            //The third argument is the amount of threads we want the pool to use
            int nThreads = Int32.Parse(args[2]);
            //The last and forth argument is the delta count
            Global.Delta = Int32.Parse(args[3]);

            //We define the set amount of threads as learned in the exercice class
            ThreadPool.SetMinThreads(nThreads, 0);
            ThreadPool.SetMaxThreads(nThreads, 0);
            Global.indexList = new List<int>();

            //We define a new stream reader object so we could read the text file
            StreamReader reader = new StreamReader(textPath);

            //We intilize the index the blocks to 0
            int blockIndex = 0;

            //While we haven't finished reading the file, or got a cancel request due to finding the string
            while (!reader.EndOfStream && !Global.cancelToken.IsCancellationRequested)
            {
                //We call for a thread to search, and wait for it to load the buffer before continuing
                ThreadPool.QueueUserWorkItem(search, new object[] { reader, blockIndex });
                blockIndex++;
                Global.waitHandle.WaitOne();
            }
            reader.Close();

            //If the index list is empty it means we didnt find the string
            if ((Global.indexList).Count == 0)
            {
                Console.WriteLine("not found");
            }
            else
                {
                Thread.Sleep(10);
                //Sort the list to get the smallest, since its possible multiple threads found it at the same time in different places
                Global.indexList.Sort();
                Console.WriteLine((Global.indexList)[0]);
                }
        }
    }
}
