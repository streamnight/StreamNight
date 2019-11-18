var channelTable = document.getElementById("status-table");
var channelTableBody = channelTable.getElementsByTagName('tbody')[0];

var connectionStatus = document.getElementById("connection-status");
var connectionAttemptsElement = document.getElementById("connection-attempts");
var connectionAttempts = 1;

var connection = new signalR.HubConnectionBuilder().withUrl("/admin/twitchhub").build();
connection.serverTimeoutInMilliseconds = 10000;

connection.on("TwitchStatus", function (twitchStatus) {
    channelTableBody.innerHTML = '';
    let twitchStatusRow = channelTableBody.insertRow(-1);
    let twitchStatusName = twitchStatusRow.insertCell(-1);
    twitchStatusName.appendChild(document.createTextNode("Twitch Enabled"));
    let twitchEnabled = twitchStatusRow.insertCell(-1);
    if (twitchStatus.twitchEnabled == true) {
        twitchEnabled.appendChild(document.createTextNode("Yes"));
    }
    else if (twitchStatus.twitchEnabled == false) {
        twitchEnabled.appendChild(document.createTextNode("No"));
    }

    for (var twitchChannel in twitchStatus.channelNames) {
        let channelRow = channelTableBody.insertRow(-1);

        let channelName = channelRow.insertCell(-1);
        channelName.appendChild(document.createTextNode(twitchStatus.channelNames[twitchChannel]));

        let deleteButtonCell = channelRow.insertCell(-1);
        let deleteButton = document.createElement("button");
        deleteButton.innerText = "Remove channel";
        deleteButton.id = twitchStatus.channelNames[twitchChannel];
        deleteButton.setAttribute("onclick", "connection.invoke('RemoveChannel', this.id)");
        deleteButtonCell.appendChild(deleteButton);
    }
});

connection.on("failed", function () {
    alert("The requested operation didn't complete successfully. Make sure the system state is valid and try again.");
});

connection.on("error", function (errorMessage) {
    alert("Error: " + errorMessage);
});

connection.onclose(disconnected);

function disconnected() {
    connectionStatus.innerText = "Data may be outdated. Attempting to reconnect to the status page...";
    connectionAttemptsElement.classList.remove("hidden");
    connect();
}

function connect() {
    connectionAttempts++;
    connection.start().then(connected).catch(function (err) {
        connectionAttemptsElement.innerText = "Connection attempts: " + connectionAttempts;
        console.error(err.toString());
        setTimeout(connect, 3000);
    });
}

function connected() {
    connection.invoke("GetTwitchStatus");
    connectionAttempts = 0;
    connectionStatus.innerText = "Connected to the status page. Changes are automatically pushed to your browser.";
    connectionAttemptsElement.classList.add("hidden");
}

function disableTwitch() {
    connection.invoke("DisableTwitch");
    alert("Disabling Twitch.");
}

function enableTwitch() {
    connection.invoke("EnableTwitch");
    alert("Enabling Twitch.");
}

function addChannel() {
    connection.invoke("AddChannel", document.getElementById("new-channel-name").value);
    document.getElementById("new-channel-name").value = "";
    alert("Adding channel.");
}

connect();