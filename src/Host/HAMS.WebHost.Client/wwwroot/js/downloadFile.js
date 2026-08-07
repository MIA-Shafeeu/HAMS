export function downloadFileFromBase64(fileName, contentType, bytesBase64) {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${bytesBase64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
