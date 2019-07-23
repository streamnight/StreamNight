"use strict";

var connectionStatus = document.getElementById("connectionStatus");

function registerTap(object, event) {
    object.addEventListener("touchstart", function () {
        isScroll = false;
    }, false);

    object.addEventListener("touchmove", function () {
        isScroll = true;
    }, false);

    object.addEventListener("touchend", function () {
        if (!isScroll) {
            event();
        }

        lastTap = Date.now();
    }, false);
}

function hideDisconnect() {
    connectionStatus.textContent = "Disconnected";
    connectionStatus.setAttribute("style", "color:red;");
    document.getElementById("viewerCount").innerText = "? In Chat";
    document.getElementById("textInput").classList.add("hidden");
    document.getElementById("reconnectButton").classList.remove("hidden");
};

function showReconnect() {
    connectionStatus.textContent = "Connected";
    connectionStatus.setAttribute("style", "color:gray;");
    document.getElementById("textInput").classList.remove("hidden");
    document.getElementById("reconnectButton").classList.add("hidden");
};

hideDisconnect();

var timeouts = 0;
var connected = false;
var reconnectButton = document.getElementById("reconnectButton");

reconnectButton.addEventListener("click", function () {
    timeouts = 0;
    connect();
});

function connect() {
    connectionStatus.textContent = "Reconnecting";
    connectionStatus.setAttribute("style", "color:orange;");

    reconnectButton.disabled = true;

    /*if (timeouts >= 5) {
        hideDisconnect();
        alert("Couldn't reconnect to chat. Click the reconnect button or refresh the page to try again.");
        document.getElementById("reconnectButton").disabled = false;
        return;
    }*/
    if (connected == true) {
        return;
    }

    connection.start().then(function () {
        showReconnect();
        connected = true;
        timeouts = 0;
    }, function (err) {
        console.error(err);
        timeouts = timeouts + 1;
        setTimeout(connect, 1000);
    });
};

var connection = new signalR.HubConnectionBuilder().withUrl("/bridgehub").build();
connection.serverTimeoutInMilliseconds = 10000;

//Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

var createMessage = function (NewMessage) {
    var chatbox = document.getElementById("chatbox");

    var li = document.createElement("li");

    var messageDiv = document.createElement("div");
    messageDiv.setAttribute("id", NewMessage.messageId);
    messageDiv.classList.add("message");

    var pfp = document.createElement("img");
    pfp.setAttribute("src", NewMessage.authorAvatar);
    pfp.setAttribute("alt", NewMessage.author + "'s profile picture");
    pfp.classList.add("profile-picture");
    messageDiv.appendChild(pfp);

    var h4 = document.createElement("h4");
    h4.textContent = NewMessage.author;
    messageDiv.appendChild(h4);

    var contentDiv = document.createElement("div");
    contentDiv.innerHTML = twemoji.parse(NewMessage.content);
    contentDiv.classList.add("message-content");
    messageDiv.appendChild(contentDiv);

    li.appendChild(messageDiv);
    chatbox.appendChild(li);

    li.scrollIntoView(false);

    var typingUserElements = Array.from(document.getElementsByClassName(NewMessage.authorId));
    typingUserElements.forEach(function (typingUserElement) {
        typingUserElement.classList.remove("typing");
    });
    var userInArray = typingUsersDisplayed.findIndex(function (element) {
        return element.userId == NewMessage.authorId;
    });
    if (userInArray > -1) {
        typingUsersDisplayed.splice(userInArray, 1);
    }
}

connection.on("NewMessage", createMessage);

connection.on("DeleteMessage", function (DeleteMessage) {
    document.getElementById(DeleteMessage.messageId).remove();
});

connection.on("EditMessage", function (EditMessage) {
    var elementToEdit = document.getElementById(EditMessage.messageId).childNodes[2];
    elementToEdit.innerHTML = twemoji.parse(EditMessage.content);
    elementToEdit.childNodes[0].setAttribute("style", "margin:0;");
    var elementImgs = Array.from(elementToEdit.getElementsByTagName("img"));

    elementImgs.forEach(function (element) {
        element.setAttribute("style", "height:2rem;margin:0;");
    });
});

connection.on("MessageHistory", function (MessageHistory) {
    MessageHistory.historyContent.forEach(createMessage);
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
    connection.invoke("GetHistory");
    showReconnect();
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("ForceDisconnect", function () {
    console.error("Remote disconnect requested. Client likely failed to respond to heartbeat request.");
    connection.stop();
});

connection.on("ForceRefresh", function () {
    location.reload(true);
});

connection.on("requestHeartbeat", function () {
    connection.invoke("heartbeatResponse");
});

connection.on("unauthorised", function () {
    console.error("Unauthorised.");
});

var sendClick = function (event) {
    var messageBox = document.getElementById("messageInput");
    var message = messageBox.value;
    connection.invoke("SendMessage", { Content: message }).catch(function (err) {
        return console.error(err.toString());
    });
    messageBox.value = "";
    event.preventDefault();
}

document.getElementById("sendButton").addEventListener("click", sendClick);
registerTap(document.getElementById("sendButton"), sendClick);


document.getElementById("messageInput")
    .addEventListener("keyup", function (event) {
        event.preventDefault();
        if (event.keyCode === 13) {
            document.getElementById("sendButton").click();
        }
    });

connection.onclose(function () {
    hideDisconnect();
    connected = false;
    connect();
})
