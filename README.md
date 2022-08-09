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

#### Mode
Can be set to `cards` or `tokens`

```json
"Mode": "tokens",
```

#### Players
Add each player target on a new line
```json
"Players": [
  "johndoe",
  "janedoe"
],
```
 
#### Cards
Add each cards details in it's own object `{}`
```json
"Cards": [
  {
    "Id": 457,
    "Bcx": 1,
    "Gold": false
  },
  {
    "Id": 236,
    "Bcx": 1,
    "Gold": true
  }
],
 ```
 
#### Tokens
Token to send can be any supported token from Splinterslands eg:. `DEC`, `SPS`, `CHAOS` etc...  
Yes.. you could even spread chaos packs if you'd like, not sure why though :-)  

Threshold: If the target has less of X token than the set threshold it will attempt to transfer the `Quantity` value.  
Quantity: The amount of X token to transfer
```json
"Tokens": {
  "Token": "DEC",
  "Threshold": 500,
  "Quantity": 100
}
```

# Run
After configuration just execute Splintermate.Delegation.exe and watch your assets fly!
