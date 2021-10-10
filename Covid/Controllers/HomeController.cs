using System;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Covid.Models;
using Microsoft.AspNetCore.Authorization;

namespace Covid.Controllers
{

    [ApiController]
    [Route("api/region")]
    public class HomeController : Controller
    {
        [HttpGet("test")] 
        public string getTest()
        {
            return "Hello Eva!";
        }

        /*
            Values for each day in the time frame selected by the filters 
            <Region> as name of region (LJ, CE, KR, ...) (foreign and unknown are not valid)
            <From> as from data
            <To> as to date
        */

        [HttpGet("cases")] // api/region/cases
        public string getCases(string Region, string From, string To)
        {
            string[] rowData = getData();
            int startCol = 1; // date | region.ce.cases.active | ... 
            // Region = null;

            DateTime startDate = new DateTime();
            DateTime fromDate = new DateTime();
            DateTime endDate = new DateTime();
            DateTime toDate = new DateTime();

            // no query From
            if (String.IsNullOrEmpty(From))
                startDate = DateTime.Parse(rowData[1].Substring(0, 10)); // start date of covid data from CSV
            else
            {
                fromDate = DateTime.Parse(From);
                startDate = fromDate;
            }

            // no query To
            if (String.IsNullOrEmpty(To))
                endDate = DateTime.Parse(rowData[rowData.Length - 2].Substring(0, 10)); // end date of covid data from CSV
            else
            {
                toDate = DateTime.Parse(To);
                endDate = toDate;
            }

            StringBuilder result = new StringBuilder();

            // LJ, CE, KR, NM, KK, KP, MB, MS, NG, PO, SG, ZA
            if (Region != null)
            {
                startCol = getRegionStartIndex(rowData[0], Region); // rowData[0] ... header

                // covid data starts in second row of CSV file
                for (int i = 1; i < rowData.Length - 1; ++i)
                {
                    DateTime currentRowDate = DateTime.Parse(rowData[i].Substring(0, 10)); // 2020-10-05, ... first 10 chars represents date
                   
                    // startDate .... currentRowDate .... endDate
                    if (DateTime.Compare(currentRowDate, startDate) >= 0 && DateTime.Compare(currentRowDate, endDate) <= 0)
                    {
                        string[] data = rowData[i].Split(','); // 2020-12-11, 3074, 13093, 251

                        /* 
                        [0] region.lj.cases.active	
                        [1] region.lj.cases.confirmed.todate	
                        [2] region.lj.deceased.todate	
                        [3] region.lj.vaccinated.1st.todate	
                        [4] region.lj.vaccinated.2nd.todate
                        */
                        result.AppendFormat("{0}: {1} [{2}, {3}, {4}, {5}]\n\n",
                            currentRowDate.ToString("dd/MM/yyyy"),  // date
                            Region.ToUpper(),                       // region
                            data[startCol],                         // number of active cases per day
                            data[startCol + 3],                     // number of vaccinated 1st
                            data[startCol + 4],                     // number of vaccinated 2nd
                            data[startCol + 2]);                    // deceased to date
                    }
                }
            }

            // Region is null (no query parameters for Region)
            else
            {
                string[] regionsName = getRegionNames();
                string[] data = null;

                for (int i = 1; i < rowData.Length - 1; ++i)
                {
                    DateTime currentRowDate = DateTime.Parse(rowData[i].Substring(0, 10));

                    if (DateTime.Compare(currentRowDate, startDate) >= 0 && DateTime.Compare(currentRowDate, endDate) <= 0)
                    {
                        result.AppendFormat("{0} ", currentRowDate.ToString("dd/MM/yyyy"));

                        data = rowData[i].Split(',');

                        for (int k = 0; k < regionsName.Length; ++k)
                        {
                            startCol = getRegionStartIndex(rowData[0], regionsName[k]);

                            result.AppendFormat("{0}: [{1}, {2}, {3}, {4}] | ",
                                regionsName[k].ToUpper(),   // region
                                data[startCol],             // number of active cases per day
                                data[startCol + 3],         // number of vaccinated 1st
                                data[startCol + 4],         // number of vaccinated 2nd
                                data[startCol + 2]);        // deceased to date
                        }
                        result.AppendFormat("\n");
                        result.AppendFormat("\n");
                    }
                }
            }
            return result.ToString();
        }

        /*
         List of regions with the sum of number of active cases in the last 7 days in descending order
        */

        [Authorize]
        [HttpGet("lastweek")] // api/region/lastweek
        public string getLastweek()
        {
            return getResult();
        }

        // get csv data from Covid-19 Sledilnik
        public static string[] getData()
        {
            string url = "https://raw.githubusercontent.com/sledilnik/data/master/csv/region-cases.csv";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();

            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();
            string[] data = text.Split('\n', '\r').ToArray(); // \n is Unix, \r is Mac, \r\n is Windows 

            // Console.WriteLine(data[data.Length]) // System.IndexOutOfRangeException
            Array.Resize(ref data, data.Length - 1);

            return data;
        }

        // get start index (header) of region
        public int getRegionStartIndex(string header, string regionName)
        {
            string name = regionName.ToLower();
            string[] headerRow = header.Split(',');
            for (int i = 0; i < headerRow.Length; ++i)
                if (headerRow[i].Contains(name))
                    return i;
            return -1;
        }

        // get number of regions
        public int getNumRegions()
        {
            string[] data = getData();
            string[] headerRow = data[0].Split(',');
            int numRegions = 0;

            for (int i = 0; i < headerRow.Length; ++i)
                if (headerRow[i].Contains("confirmed.todate"))
                    numRegions++;
            return numRegions;
        }

        // get region names
        public string[] getRegionNames()
        {
            string[] data = getData();
            string[] headerRow = data[0].Split(','); // header row
            string[] regionNames = new string[headerRow.Length];
            string[] columnWord = null;
            string[] names = null;

            for (int i = 1; i < headerRow.Length; ++i)
            {
                columnWord = headerRow[i].Split('.'); // first column
                regionNames[i - 1] = columnWord[1].ToUpper();
            }
            Array.Resize(ref regionNames, regionNames.Count() - 1); // !!! (for zanka)

            // delete duplicates names
            string[] regNames = regionNames.Distinct().ToArray();

            // delete unvalid region names
            var newRegionNames = new List<string>(regNames);

            newRegionNames.Remove("UNKNOWN");
            newRegionNames.Remove("FOREIGN");

            names = newRegionNames.ToArray();

            return names;
        }

        public string getResult()
        {
            string[] data = getData();
            string[] headerRow = data[0].Split(',');

            int numRegions = getNumRegions();

            string[] regionNames = new string[numRegions];
            int[] activeColumns = new int[numRegions];
            string[] colWords = null;

            int iactiveCol = 0;

            // fill up arrays for "cases.active"
            for (int col = 0; col < headerRow.Length; ++col)
            {
                if (headerRow[col].Contains("cases.active"))
                {
                    colWords = headerRow[col].Split('.'); // region.kk.cases.active
                    activeColumns[iactiveCol] = col;
                    regionNames[iactiveCol] = colWords[1]; // kk
                    iactiveCol++;
                }
            }

            // sum of number of active cases in the last 7 days for each region
            int[] sumActiveColumns = new int[numRegions];
            for (int i = data.Length - 7; i < data.Length; ++i)
            {
                string[] currentRow = data[i].Split(',');
                for (int col = 0; col < activeColumns.Length; ++col)
                {
                    int activeCol = activeColumns[col];
                    sumActiveColumns[col] += int.Parse(currentRow[activeCol]);
                }
            }

            // sort in descending order
            for (int i = 0; i < regionNames.Length; ++i)
            {
                for (int j = i + 1; j < regionNames.Length; ++j)
                {
                    // compare array element with all next element
                    if (sumActiveColumns[i] < sumActiveColumns[j])
                    {

                        int temp1 = sumActiveColumns[i];
                        sumActiveColumns[i] = sumActiveColumns[j];
                        sumActiveColumns[j] = temp1;

                        string temp2 = regionNames[i];
                        regionNames[i] = regionNames[j];
                        regionNames[j] = temp2;
                    }
                }
            }

            // print regions with the sum of number of active cases in the last 7 days in descending order
            StringBuilder result = new StringBuilder();
            StringBuilder trash = new StringBuilder();
            for (int i = 0; i < regionNames.Length; ++i)
            {
                if (String.Compare(regionNames[i], "unknown") == 0 || String.Compare(regionNames[i], "foreign") == 0)
                    trash.AppendFormat("");
                else
                    result.AppendFormat("Region {0} has {1} of active cases in the last 7 days.\n",
                        regionNames[i].ToUpper(),
                        sumActiveColumns[i]);
            }
            return result.ToString();
        }
    }
}
