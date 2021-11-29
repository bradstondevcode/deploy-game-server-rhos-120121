var uuid = require('uuid-random');
const WebSocket = require('ws')

const wss = new WebSocket.WebSocketServer({port:8080}, ()=> {
  console.log('server started')
})

//Object that stores player data 
var playersData = {
  "type" : "playersData"
}

//=====WEBSOCKET FUNCTIONS======

//Websocket function that manages connection with clients
wss.on('connection', function connection(client){

  //Create Unique User ID for player
  client.id = uuid();

  console.log(`Client ${client.id} Connected!`)

  playersData[""+client.id] = {position: {} }

  var currentClient = playersData[""+client.id]

  //Send default client data back to client for reference
  client.send(`{"id": "${client.id}"}`)

  //Method retrieves message from client
  client.on('message', (data) => {
    var dataJSON = JSON.parse(data)

    var dataKeys = Object.keys(dataJSON)

    dataKeys.forEach(key => {
      playersData[dataJSON.id].position[key] = dataJSON[key]
    });

    console.log(playersData[dataJSON.id].position)

    var tempPlayersData = Object.assign({}, {}, playersData)

    var keys = Object.keys(tempPlayersData)

    //Remove "type" from keys array
    keys = removeItemOnce(keys, "type")

    tempPlayersData["playerIDs"] = keys

    client.send(JSON.stringify(tempPlayersData))
  })

  //Method notifies when client disconnects
  client.on('close', () => {
    console.log('This Connection Closed!')

    console.log("Removing Client: " + client.id)

    //Iterate over all clients and inform them that a client with the specified ID has disconnected
    wss.clients.forEach(function each(cl) {
          if (cl.readyState === WebSocket.OPEN) {
            console.log(`Client with id ${client.id} just left`)
            //Send to client which other client (via/ id) has disconnected
            cl.send(`Closed:${client.id}`);
          }
      });

    //Remove disconnected player from player data object
    delete playersData[""+client.id]

    console.log(playersData)

  })

})

wss.on('listening', () => {
  console.log('listening on 8080')
})

//=====UTILITY FUNCTIONS======

function removeItemOnce(arr, value) {
  var index = arr.indexOf(value);
  if (index > -1) {
    arr.splice(index, 1);
  }
  return arr;
}
