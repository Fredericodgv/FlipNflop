mergeInto(LibraryManager.library, {
  UploadJSON: function(objectName, callbackMethod) {
    var obj = UTF8ToString(objectName);
    var cb  = UTF8ToString(callbackMethod);

    var input = document.createElement("input");
    input.type = "file";
    input.accept = ".json";

    input.onchange = function(e) {
      var file = e.target.files[0];
      if (!file) return;

      var reader = new FileReader();
      reader.onload = function(evt) {
        // Envia o conteúdo JSON de volta ao objeto Unity indicado
        SendMessage(obj, cb, evt.target.result);
      };
      reader.readAsText(file);
    };

    input.click();
  }
});