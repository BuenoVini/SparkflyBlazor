# Sparkfly Blazor
Sparkfly is an open source app that allows you and your friends to listen to music together using the Spotify queue.

## Main goal
When I started my internship in 2022, I heard people talking about Blazor and how easy you could create Single Page Applications with it. Since I decided to learn C# in 2022, I was thinking about a project I could build to put my skills in practice. Allied with my passion for listening to music, I was very pumped to start building this application and seeing my friends using it.

The main ideia behind Sparkfly was to create a service that would solve a common problem that my friends from college and from work were having.

Every Friday, when I am commuting to college, the van's driver allows us to listen to [Spotify](https://www.spotify.com/br/). Concidently, every Friday morning at work, we listen to some music in the office too. The issue is that if you want to suggest a song to play, you would have to ask the owner of the Spotify account which songs to play next. That's when I had the idea to create Sparkfly.

This project was made with many sleepless nights but with a lot of love, for sure!

## Take away

Just like my [last project](https://github.com/BuenoVini/lastfm-analyzer), this repository is simply a way to showcase my programming skills and what I have learned during the development process.

New topics **learned** (by no means exhaustive):
- Blazor Server
- SignalR
- MudBlazor UI
- Local and Session Storage
- Localization in Blazor Apps
- Async methods
- Events and Delegates
- Custom Exceptions
- Dependency Injection
- Difference between Singleton and Scoped Services
- Hosting Web Apps on Microsoft Azure

But most importantly, this is the first peace of software that I wrote that other people have actually used and gave me feedback. It was curious, to say the least, seeing my friends trying to use the app and doing something that they shouldn't be doing and then breaking it. Or simply not knowing how to use it because some features weren't clearer.

The two events that I will always remember are:
- When a college friend tried to click the magnifying glass icon on the search bar expecting it to be a button (it was simply an adornment).
- When a friend from work added the same track to the queue ten times because evertime he clicked the add button nothing in the UI told him that the track was successfully added.

They were such an obvious behaviour to expect from this features, but I simply didn't even think about them when developing the app.
I later changed them based on their feedback in [made adornment on search bar clickable](https://github.com/BuenoVini/SparkflyBlazor/commit/ed6ed54f435caddedb4a7c52c55b3d1da983b9f4) and [added validation when voting for a track already voted](https://github.com/BuenoVini/SparkflyBlazor/commit/3767311f498bb2b4a633478270fc1dafd323c9d7).

## Features
Using MudBlazor's navigation bar, the user can access the main functionalities of the app, such as:
- Show the currently playing song on Spotify and peek the next track to be played.
- Check which tracks they and their friends have voted for. It is also possible to remove a track suggestion if the user wishes to.
- Search for tracks to add to the Sparkfly queue.

The queue on Sparkfly has a priority system to manage the users' suggestions. The idea is that if a user decides to queue ten tracks, but another makes a suggestion for the first time, then this new track will have priority. The main goal is to avoid a brand new vote playing last because a friend of yours already added a bunch of tracks on the queue. On Sparkfly, everyone's songs will be played!

| ![image4](https://user-images.githubusercontent.com/51279927/192420863-54058ac9-7890-40dd-837f-8a01822aa088.jpeg)  | ![image1](https://user-images.githubusercontent.com/51279927/192420374-41475866-d725-49fa-8be0-cea8877558ea.jpeg) | ![image2](https://user-images.githubusercontent.com/51279927/192420506-27e98e30-dad8-4aa9-b402-ec27b3cfbedf.jpeg) | ![image3](https://user-images.githubusercontent.com/51279927/192420847-0ba93987-ab40-4344-852f-78c2b05bedc4.jpeg) | ![image5](https://user-images.githubusercontent.com/51279927/192421016-8dc009b2-2410-4ba5-9a81-067b3761de59.jpeg) | 
| ------------- | ------------- | ------------- | ------------- | ------------- |
|  |  |  |  |  |


## Contributing
You cannot modify or redistribute this code without explicit permission.

This repository is for practicing my skills only and does not represent a final product.

### Disclaimer
Some of the features created here are already implemented on Spotify, such as [Remote group session](https://support.spotify.com/us/article/remote-group-session/). With this project, I have no intention whatsoever of demonetizing Spotify. That's why I didn't share here a link of the app's URL.

As stated above, this is a mere student project to showcase my programming skills.

Sign in for an [Spotify Premium account](https://support.spotify.com/us/article/premium-plans/).

**Do not** use this repository as a way to circumvent their subscription plan.

## Credits and Acknowledgement
Since I was learning Blazor as I was building this app, here are a few documents that helped me during development:
- Blazor Overview: https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-6.0
- Build web applications with Blazor (Learning Path): https://learn.microsoft.com/en-us/training/paths/build-web-apps-with-blazor/
- Asynchronous programming with async and await (amazing reading!): https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/
- Spotify's Authorization Code Flow (Web API): https://developer.spotify.com/documentation/general/guides/authorization/code-flow/
- Blazor state management: https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management?view=aspnetcore-6.0&pivots=server
- MudBlazor (A Component Library): https://mudblazor.com/

And here is a BIG THANKS to all people that, one way or the other, helped me along the way!

A special thanks to Renan Soares ([Renans0ares](https://github.com/Renans0ares)) who introduced me to MudBlazor and made my *"front-end life"* much easier and to Léo Azevedo ([LeoAzevedo59](https://github.com/LeoAzevedo59)) who helped me a big time with code design and Azure Cloud. **You guys are legend!!**

Also, to all of my friends from college and from work that kept my motivation for this project alive! I made Sparkfly to all of you, after all!

To my mom, who has the biggest patience in the world!

And to Rafa, who kept my brittle heart warm.

## Next Steps
In the next few months I plan to come back to this project and update a few things as they are needed.

From the top of my head:
- Allow multiple users to connect with their Spotify Premium account. (Difficult and time consuming...)
- Create a sharable link so that guests can easily join in. (Somewhat doable)
- Add a page to display the current track lyrics. (Easy but requires testing. This was supposed to be on v1 but I had to cut this feature off)
- Fix some edge case scenarios in the Priority Queue. (Easy but needsa a lot of testing)
