Sample shows how to run the .NET gRPC client secured by the TLS Certificate.

## 1. Generate self-signed certificates
Use following command to generate certificates:
- Linux/MacOS
	```console
	./create-certs.sh
	```
- Windows 
	```powershell
	.\create-certs.ps1
	```
_Note: to regenerate certificates you need to remove the [./certs](./certs) folder._

## 2. Run samples with Docker
Use following command to run samples with Docker:
```consoler
docker-compose up
```
It will run both server and client with preconfigured TLS connection setup.

## 3. Run run samples locally (without Docker)
To run samples locally you need to install generated CA certificate. 

### 3.1 Install certificate - Linux (Ubuntu, Debian)
- Copy [./certs/ca/ca.crt](./certs/ca/ca.crt) to dir `/usr/local/share/ca-certificates/`
- Use command: 
  ```console
  sudo cp foo.crt /usr/local/share/ca-certificates/foo.crt
  ```
- Update the CA store: 
  ```
  sudo update-ca-certificates.
  ```
### 3.2 Install certificate - Windows
Windows certificate will be automatically installed when [.\create-certs.ps1](.\create-certs.ps1) was run.

### 3.2 Run application
Run application from console:

```console
dotnet run .\secure-with-tls.csproj
```
or from your favourite IDE.


