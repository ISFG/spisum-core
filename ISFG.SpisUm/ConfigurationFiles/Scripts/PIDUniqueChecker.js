var pid = document.properties["ssl:pid"];

if (pid) {

    var result = search.query({ query: "=ssl\\:pid:'" + pid + "'", language: "fts-alfresco" });

    if (result.length > 1) {
        throw new Error();
    }

}