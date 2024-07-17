# Introduction

Render is a tool developed by Faith Comes By Hearing for oral bible
translation.  With LaunchPad and Couchbase (see below), render supports
configurable, distributed asynchronous translation workflows, so projects
can configure workflows which best meet their needs, and team members can
perform work online or offline, and sync as network is available.

The project is transitioning from closed to open source, initially via
release snapshots
([cathedral](https://en.wikipedia.org/wiki/The_Cathedral_and_the_Bazaar)
model now, perhaps bazaar later).

Render is trademarked, if you wish distribute a revised version of this app, you 
may not call it Render, nor use the Render Icon.

This release includes the ability to create a demo build which includes a
working project.  However, although all sync code is included, access to
our sync servers is not - so the build will functionally be stand-alone
(without sync), which you can revise, test, submit pull requests, etc.

For sync, you'll need to setup a [Couchbase](https://www.couchbase.com/)
service, or revise render to use something else.  In addition, render
projects are created via a separate web app called LaunchPad, which we will
also open-source, or you could revise render to use something else.

So this initial release does not include everything needed to immediately
duplicate a production infrastructure, but it is a significant first step
toward collaborative open development.  

See [Render documentation](doc/RENDER_DOC.md) to get started.

For technical details see [Developer documentation.](doc/DEVELOPMENT_DOC.md) 

Render uses many other software packages, see OtherLicenses for their terms
of use.

The Render application is distributed under the MIT license. Refer to the full license text at the following link: [The Render application license.](LICENSE.md)

The Render application utilizes many third-party packages with their own distribution licenses. Refer to the full list of third-party package licenses at the following link: [Packages licenses.](OtherLicenses/.index.md)