# S3 Transport

Single Put and Get are the only things are offer.  Using a background process like MongoTransport is used.

## To Use
- Add to your environment variables or hardcore the values in the code.
- Env vars:
  - ACCESS_KEY
  - SECRET_KEY
  - REGION
  - BUCKET_NAME
  - PATH

## Notes

- MongoTransport.ConsumeQueue doesn't use the i variable when consuming the queue.
- Utilities.WaitUntil is used but Task.Run is used and creates a new thread.  Maybe this is desired.
- GetObject isn't async and S3 is only async so getting is sub-optimal.
- API should be changed to be properly ASYNC but that's major breakage.
- Possibly a unified way to background process any transport.

## Future work

- Add Azure Blob Storage
- Add Google Cloud Storage
