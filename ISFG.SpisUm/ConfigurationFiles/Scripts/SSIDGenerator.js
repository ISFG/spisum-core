var runScript = !document.properties["ssl:ssidNumber"]
    && document.typeShort == "ssl:document";

if (runScript) {
    var ssidNumber = 0;

    if (!companyhome.hasAspect("ssl:ssid_counter")) {
        var props = new Array(1);
        props["ssl:ssid_counter_value"] = ssidNumber;
        companyhome.addAspect("ssl:ssid_counter", props);
        companyhome.save();
    }

    ssidNumber = parseInt(companyhome.properties["ssl:ssid_counter_value"]);

    while (true) {
        ssidNumber++;
        companyhome.properties["ssl:ssid_counter_value"] = ssidNumber;
        companyhome.save();

        var result = search.query({ query: "=ssl\\:ssidNumber:'" + ssidNumber + "'", language: "fts-alfresco" });

        if (result != "") {
            continue;
        }

        document.properties["ssl:ssidNumber"] = ssidNumber;
        document.save();

        break;
    }
}