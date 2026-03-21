import os
import logging
import uvicorn
from fastapi import FastAPI, HTTPException
from starlette.responses import FileResponse, PlainTextResponse
from starlette.status import HTTP_404_NOT_FOUND, HTTP_500_INTERNAL_SERVER_ERROR, HTTP_400_BAD_REQUEST

UPDATE_FOLDER = 'updates'
VERSION_FILE = 'version.txt'

logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')

app = FastAPI(title="Ink Canvas Artistry Update Server")

base_dir = os.path.abspath(os.path.dirname(__file__))
update_dir = os.path.join(base_dir, UPDATE_FOLDER)
version_file_path = os.path.join(base_dir, VERSION_FILE)


def is_safe_update_filename(filename: str) -> bool:
    normalized_filename = os.path.normpath(filename)
    return bool(filename) and normalized_filename not in ("", ".") and not os.path.isabs(filename) and os.path.basename(normalized_filename) == normalized_filename


def resolve_update_file(filename: str) -> str | None:
    if not is_safe_update_filename(filename):
        return None

    normalized_filename = os.path.normpath(filename)

    try:
        for entry in os.scandir(update_dir):
            if entry.is_file() and entry.name == normalized_filename:
                return entry.path
    except OSError as e:
        logging.error(f"Could not enumerate update files: {e}")
        raise HTTPException(status_code=HTTP_500_INTERNAL_SERVER_ERROR, detail="Could not read update directory.")

    return None

if not os.path.isdir(update_dir):
    logging.warning(f"Update directory '{update_dir}' not found. Creating it.")
    try:
        os.makedirs(update_dir, exist_ok=True)
    except OSError as e:
        logging.error(f"Could not create update directory: {e}")

if not os.path.isfile(version_file_path):
     logging.error(f"Version file '{version_file_path}' not found.")
     try:
         with open(version_file_path, 'w') as f:
             f.write("0.0.0\n")
         logging.info(f"Created default version file '{version_file_path}' with version 0.0.0.")
     except IOError as e:
         logging.error(f"Could not create default version file: {e}")


@app.get("/version", response_class=PlainTextResponse)
async def get_version():
    logging.info("Request received for /version")
    try:
        with open(version_file_path, 'r') as f:
            version = f.read().strip()
            logging.info(f"Serving version: {version}")
            return version
    except FileNotFoundError:
        logging.error(f"Version file not found at {version_file_path}")
        raise HTTPException(status_code=HTTP_404_NOT_FOUND, detail="Version file not found.")
    except IOError as e:
        logging.error(f"Error reading version file: {e}")
        raise HTTPException(status_code=HTTP_500_INTERNAL_SERVER_ERROR, detail="Could not read version file.")
    except Exception as e:
        logging.error(f"Unexpected error reading version file: {e}")
        raise HTTPException(status_code=HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error.")


@app.get("/download/{filename}")
async def download_file(filename: str):
    logging.info(f"Request received for /download/{filename}")

    normalized_filename = os.path.basename(os.path.normpath(filename))
    if not is_safe_update_filename(filename):
        logging.warning(f"Attempted path traversal: {filename}")
        raise HTTPException(status_code=HTTP_400_BAD_REQUEST, detail="Invalid filename.")

    file_path = resolve_update_file(filename)
    if file_path is None:
        logging.error(f"File not found in update directory: {normalized_filename}")
        raise HTTPException(status_code=HTTP_404_NOT_FOUND, detail=f"File '{normalized_filename}' not found.")

    logging.info(f"Attempting to send file from path: {file_path}")

    try:
        return FileResponse(path=file_path, filename=normalized_filename, media_type='application/octet-stream')
    except Exception as e:
        logging.error(f"Error serving file {filename}: {e}")
        raise HTTPException(status_code=HTTP_500_INTERNAL_SERVER_ERROR, detail="Error serving file.")


if __name__ == "__main__":
    logging.info("Starting FastAPI server with Uvicorn...")
    uvicorn.run("server:app", host="0.0.0.0", port=8080, reload=False, log_level="info")
