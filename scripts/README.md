Updating WebService client references

Where possible services have been consolidated to reduce duplication and simplify maintenance.
However, some services have had to be seperated due to type name clashes - specifically Certex. 

Common functionality is provided in scripts/_generate-webservices.sh, which is called by per-service thin wrappers.

The dotnet svcutil params file must be named dotnet-svcutil.params.json and is used per-service to control namespace mappings and inputs. Keeping a params.json in each service folder prevents name collisions and makes per-service namespace configuration trivial.

To regenerate clients, from the repository root run the wrapper for the service:

  ./scripts/core/update-webservices.sh
  ./scripts/certex/update-webservices.sh

To extend an existing client, edit the WSDL and params.json in the service folder, then run the wrapper script.

What the scripts do
- Copy the service params.json and WSDL into a temporary workdir
- Invoke dotnet-svcutil in that workdir (expects dotnet-svcutil to be available)
- Use FileSplitter.csx to split the combined generated file into per-type files
- Replace generated files in the target project folder

Adding a new service client
1. Create scripts/<service-name>/
2. Add a master.wsdl and a dotnet-svcutil.params.json with appropriate namespace mappings (keep the filename exactly)
3. Add a minimal update-webservices.sh that calls ../_generate-webservices.sh with three args: WSDL path, params dir, output dir
