---
layout: web-api
currentMenu: web-api
currentSubMenu: cron-trigger
---

## CronTrigger Endpoint

The CronTrigger endpoint allows for an integration job to be scheduled by creating and manipulating a Quartz.NET CronTrigger.

### Parameters

<div class="table-responsive">
<table class="table table-bordered">
<thead><tr><th>Param</th><th>Type</th><th>Details</th></tr></thead>
<tbody>
    <tr><td>Id</td><td><span class="label label-info">string</span></td>
        <td>A unique, auto-generated identifier for the trigger.</td>
    </tr>
    <tr><td>JobType</td><td><span class="label label-info">string</span></td>
        <td>The <a href="http://msdn.microsoft.com/en-us/library/system.type.fullname%28v=vs.110%29.aspx">FullName</a> of an <a href="integration-jobs.html">Integration Job</a> type.</td>
    </tr>
    <tr><td>StateId</td><td><span class="label label-danger">int</span></td>
        <td>An integer identifier that sets the state of the trigger. Valid values are 0 (active) and 1 (paused).</td>
    </tr>
    <tr>
        <td>Parameters</td>
        <td><a href="https://msdn.microsoft.com/en-us/library/s4ys34ea%28v=vs.110%29.aspx">System.Collections.Generic.IDictionary<string,string></a></td>
        <td>A key/value object that is made available to integration jobs that implement the _IParameterizedJob_ interface.</td>
    </tr>
    <tr><td>CronExpressionString</td><td><span class="label label-info">string</span></td>
    <td>A valid <a href="http://en.wikipedia.org/wiki/Cron#CRON_expression">cron expression</a>.</td>
    </tr>
</tbody>
</table>
</div>

### Get a List of CronTriggers
GET api/CronTrigger
```sh
curl http://localhost:9001/api/CronTrigger
```

### Get a Specific CronTrigger by ID
GET api/CronTrigger/ID
```sh
curl http://localhost:9001/api/CronTrigger/ID
```

### Create a New CronTrigger
POST api/CronTrigger
```sh
curl --data "JobType=IntegrationServer.Car.CarMailMessageJob&CronExpressionString=0 3 4 ? * MON-FRI *" http://localhost:9001/api/CronTrigger
```

### Update a Specific CronTrigger by ID
PUT api/CronTrigger/ID
```sh
curl -XPUT "CronExpressionString=0 4 1 ? * MON-FRI *" http://localhost:9001/api/CronTrigger/ID
```

### Delete a CronTrigger by ID
DELETE api/CronTrigger/ID
```sh
curl -XDELETE http://localhost:9001/api/CronTrigger/ID
```
