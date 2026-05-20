# TracesNT WCF WebServices

This library contains the consolidated WCF client proxies and shared data models for interfacing with the European Commission's Trade Traces network.

## Architecture
The API endpoints are auto-generated from a unified `master.wsdl` file to enforce deduplication of shared XML data contracts (like `SPSPartyType`).

By importing all EU endpoints natively into a single `.wsdl` file, `dotnet-svcutil` scans the full schema graph natively in memory. This deliberately bypasses a known .NET Core limitation regarding cross-assembly XML serialization, yielding precisely *one* definitive POCO object per shared data contract, mapped perfectly across all WCF clients.

## Managing the Contracts

If you need to expand capabilities to additinal Traces schemas, or synchronize schema updates:

1. **Register the Endpoint**: Open `master.wsdl` and map the new service using a `<wsdl:import>` tag pointing directly at its WDSL endpoint.
2. **Synchronize Code**: Run the update script from the repository root to regenerate the proxy files:

```bash
./scripts/update-webservices.sh
```

*(Note: During schema generation, it is normal to see minor schema warnings like `Validation Error: The global attribute 'http://www.w3.org/XML/1998/namespace:lang' has already been declared`. These emerge harmlessly from standard W3C definitions embedded in EU schemas and do not impact generation).*
