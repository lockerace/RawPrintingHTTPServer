# Raw Printing HTTP Server
 Simple HTTP Server to send binary data from browser (javascript) to client local or network printer. It supports ESC/POS codes and works well with Generic/Text Only Driver. It supports all browser that has XHR implementation.
 
## How It Works
<pre>
      Cloud (API)
  
           &#x2193;   send Print Document (Base64 String)

        Browser
        
           &#x2193;   Relay the print document
           
Raw Printing HTTP Server

           &#x2193;   Convert Base64 to byte array then send

Local or Network Printer
</pre>

## Installation
* Download the zip package [here](https://github.com/lockerace/RawPrintingHTTPServer/releases/latest)
* Extract the zip package
* Run setup.exe
* Follow the steps
* Run Raw Printing HTTP Server from Start (Auto start whenever user sign in)

## Configurations
Config file will be in `%PROGRAMDATA%\lockerace\RawPrintingHTTPServer` named `config.json`.
parameters :
* allowedDomains: string[] => list of allowed origin domains (please include protocol & port)
* port: int => port number to listen (default: 9100)

example :
```
{
    "allowedDomains": [
        "http://localhost:4200",
        "http://localhost:3000"
    ],
    "port": 9100
}
```
## Endpoints :
* GET `/` => view current status of the server and remove already allowed domains via GUI
response :
    * type: `html`

* GET `/printers` => list all printers available at current machine. HTTP request header origin must be within allowed domains.

   response :
    * type: `json`
    * values:
       * machineName: string => NetBIOS resolved computer name
       * printers: string[] => list of printer names installed locally
    * example :
`{ "machineName" : "Somebody-PC", "printers" : ["Microsoft XPS Document Writer", "Fax"] }`

* GET `/permissions` => check whether origin allowed to print

   response :
   * type: `json`
   * values:
      * allowed: boolean => whether origin allowed to print or not
   * example :
   `{ "allowed": true }`

* GET `/permissions` => ask local user permission to allow requested domain to print

   query parameters :
   * h: string (required) => domain origin that will be allowed to print which include protocol & port (URI encoded). e.g.: `http%3A%2F%2Flocalhost%3A4200`

   response :
   * type: `html`
   * description: dialog with 2 buttons, allow and block

* POST `/` => send print job information to printer

   accept: `json`

   body parameters :
   * printer: string (required) => printer name
   * data: string (required) => base64 encoded binary data
   * id: string (required) => document id/print job name

   response :
   * type: `json`
   * values:
      * success: boolean => print job sent status
      * data: string => document id/print job name if success or error message if failed

   example :
`{ "printer": "LX-300+", "data": "SGVsbG8gV29ybGQh", "id": "Test Document" }`

### Notes
* For network printer, you must install it locally first (drivers & authentication)
* You can use [esc-pos-encoder](https://github.com/NielsLeenheer/EscPosEncoder) to generate binary data (works with either Node.js or browser)

## Usage
[Angular](https://github.com/angular/angular) + [esc-pos-encoder](https://github.com/NielsLeenheer/EscPosEncoder):
```typescript
@Component({
  selector: 'app-root',
  template: `
      <div *ngIf="message">{{message}}</div>
      <button (click)="print()">Print</button>
  `})
class AppComponent {
  message: string;
  constructor(private http: HttpClient) {}
  print() {
    // Generate the document to print
    let encoder = new EscPosEncoder();
    let result = encoder
      .initialize()
      .text('The quick brown fox jumps over the lazy dog')
      .newline()
      .encode();
    let docId = 'Test Document';
    
    // Convert UInt8Array to base64 string
    let buf = Buffer.from(result.buffer);
    let binData = buf.toString('base64');
    
    // Send print job to printer
    this.http.post('http://localhost:9100', {printer: 'LX-300', data: binData, id: docId})
      .subscribe((response) => {
        if (response.success) {
          this.message = 'Print job sent successfully';
        } else {
          this.message = response.data;
        }
      }, (err) => {
        // If failed, ask for print permission
        let winOpt = 'toolbar=no,location=no,directories=no,status=no,menubar=no,scrollbars=yes,resizable=yes,width=400,height=300,top=0,left=0';
        window.open('http://localhost:9100/permissions?h=http%3A%2F%2Flocalhost%3A4200', 'Ask Permission', winOpt);
      });
  }
}
```

# Credits
[Michael Davies](https://www.codeproject.com/Tips/704989/Print-Direct-To-Windows-Printer-EPOS-Receipt)
