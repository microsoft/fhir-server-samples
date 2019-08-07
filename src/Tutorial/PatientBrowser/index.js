var express = require('express');
var app = express();
var cors = require('cors');
var morgan = require('morgan');
var path = require('path');

var port = 30662;
app.use(morgan('dev'));
app.use(cors());
app.use(express.static('JavaScriptSPA'));
app.get('*', function(req,res) {
    res.sendFile(path.join(__dirname + '/index.html'));
});

app.listen(port);
console.log('Listening on port ' + port + '...');