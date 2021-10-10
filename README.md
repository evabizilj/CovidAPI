# Covid API

Covid API is .NET web API that exposes the Slovenian Covid statistics.

We use the latest data available online from COVID-19 sledilnik (https://raw.githubusercontent.com/sledilnik/data/master/csv/region-cases.csv).

## Endpoints

API exposes two RESTful endpoints: 

### api/region/cases
It supports optional query parameters (filters):
  * Region: possible values (LJ, CE, KR, NM, KK, KP, MB, MS, NG, PO, SG, ZA)
  * To: from date
  * From: to date

Resultset contains date, region, number of active cases per day, number of vaccinated 1st, number of vaccinated 2nd and deceased to date for each day in the time frame.

### /api/region/lastweek
Resultset contains a list of valid regions with the sum of number of active cases in the last 7 days for each (sorted in a descending order). 


## API Authentication
We use JSON Web Token and Ath0 (https://auth0.com).

