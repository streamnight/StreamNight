var twitchStreams = document.getElementById("twitchStreams");
var streamContainer = document.getElementById("streamContainer");
var twitchChats = document.getElementById("twitchChat");
var mainChat = document.getElementById("mainChat");

function openTab(tabName) {
    var streamItemName = "stream-" + tabName;
    var chatItemName = "chat-" + tabName;
    var streams = Array.from(document.getElementsByClassName("streamItem"));

    var currentTwitchIndex = twitchPlayers.findIndex(function (element) {
        return element._bridge._playerState.channelName == tabName;
    });

    for (var i = 0; i < twitchPlayers.length; i++) {
        if (i == currentTwitchIndex) {
            twitchPlayers[i].play();
        }
        else {
            twitchPlayers[i].pause();
        }
    }

    streams.forEach(function (streamItem) {
        if (streamItem.id != streamItemName &&
            streamItem.id != chatItemName &&
            !streamItem.classList.contains("hidden")) {
            // Whitespace to make it easier to read
            streamItem.classList.add("hidden");
        }
    });

    var tabToActivate = document.getElementById(streamItemName);
    var chatToActivate = document.getElementById(chatItemName);
    if (tabToActivate != null && tabToActivate.classList.contains("hidden")) {
        tabToActivate.classList.remove("hidden");
    }
    if (chatToActivate != null && chatToActivate.classList.contains("hidden")) {
        chatToActivate.classList.remove("hidden");
    }

    if (tabName == "streamContainer") {
        twitchStreams.classList.add("hidden");
        twitchChats.classList.add("hidden");
        streamContainer.classList.remove("hidden");
        mainChat.classList.remove("hidden");

    }
    else if (tabName != "streamContainer") {
        streamContainer.classList.add("hidden");
        twitchStreams.classList.remove("hidden");
    }
}

function switchChat() {
    if (streamContainer.classList.contains("hidden")) {
        twitchChats.classList.toggle("hidden");
        mainChat.classList.toggle("hidden");
    }
    else {
        mainChat.classList.remove("hidden");
        alert("Can't show Twitch chat for this stream.");
    }
}