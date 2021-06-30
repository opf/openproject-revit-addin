# OpenProject Revit Add-In

## Intro

**This Software is still in Alpha status and not yet officially released. Please use it at your own risk.**

The _OpenProject Revit Add-In_ allows you to use the open source project management software _OpenProject BIM_ directly
within your Autodesk Revit environment. It lets you create, inspect and manage issues right in the moment when you can
also solve them - when you have your Revit application fired up and the relevant BIM models open. Issues get stored as
BCFs centrally and are available to every team member in real time - thanks to our browser based IFC viewer even to
those team members without expensive Revit licenses. No BCF XML import/export is needed. However, you still can import
and export BCF XML as you like and stay interoparable with any other BCF software.

This program originally based on the excellent [BCFier](https://github.com/teocomi/bcfier) but then moved into a new
direction.

## Installation

Please follow the [installation instructions](docs/installation-instructions.md).

## Development

### Browser Developer Tools

We can enable the **Developer Tools** for the `CefSharp` browser of the Windows application. The add-in creates on first
run a default configuration file at `~\AppData\Roaming\OpenProject.Revit\OpenProject.Configuration.json`. To enable the
developer tools, change the value of `EnableDevelopmentTools` to `true`.

## License

GNU General Public License v3 Extended This program uses the GNU General Public License v3, extended to support the use
of BCFier as Plugin of the non-free main software Autodesk Revit.
See <http://www.gnu.org/licenses/gpl-faq.en.html#GPLPluginsInNF>.

Copyright (c) 2013-2016 Matteo Cominetti

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public
License as published by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied
warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not,
see <http://www.gnu.org/licenses/gpl.txt>.
