# dfe-analytics-dotnet development

## Making changes

Ensure all changes are captured in CHANGELOG.md.

Add entries under an "Unreleased" section, and move these to a new section with the version number when you release.
See [this commit](https://github.com/DFE-Digital/dfe-analytics-dotnet/commit/ac068a185e7fbcdd8b576204bd5853bfc6d66640) for an example.


## Testing pre-release versions

Every build in CI produces a set of packages. These packages are available as a 'Packages' build artifact on the GitHub actions run.
These packages can be downloaded and added to a local NuGet feed for testing in applications before they're fully released to NuGet.org.


## Releasing a new version

The three packages - `DfeAnalytics.Core`, `DfeAnalytics.AspNetCore` and `DfeAnalytics.EFCore` - are all released together with the same version number.

To create a release, determine the release's version number (following [semantic versioning](https://semver.org/)) and update the CHANGELOG.md file with this version number.
Ensure this change is in the `main` branch and that the CI build has completed successfully.

On the GitHub repository, create a new release with the chosen version number and create a new tag with the same version number.
For example, if the new version is 0.5.0, the tag should be `v0.5.0` and the release should be titled `v0.5.0`.
Copy the release notes for the new version from the CHANGELOG.md file into the release description.

The build will capture the version number from the tag and use this to name the packages it produces, so it's important that the tag is correct.

When the release is published, GitHub actions will publish the packages to NuGet.org. There can be a short delay until the packages are available to consume.
