# Splintermate.Delegation
Card and token delegation solution for Splinterlands

## Prerequisites
Install .NET 6 Runtime:  
https://dotnet.microsoft.com/en-us/download/dotnet/6.0

## Configuration
There's 2 configuration files:  

### appsettings.json
Only required for changing the delay (in milliseconds) per delegation action.  
If you set this value too low and have a lot of accounts to delegate to you will run in rate-limits from the Splinterlands API's

### delegation.json
The main configuration file of this solution.  

Depending on the delegation type you're going to execute you'll need to provide the proper keys:
- Card delegation: Posting key
- Token transfer: Active key

#### Account
Username: of the account to delegate/transfer from in lowercase eg:. johndoe  
PostingKey: Private WIF posting key  
ActiveKey: Private WIF active key  

```json
"Account": {
  "Username": "", 
  "PostingKey": "", 
  "ActiveKey": "" 
},
```

# Run
After configuration just execute Splintermate.Delegation.exe and navigate to http://127.0.0.1:5237
