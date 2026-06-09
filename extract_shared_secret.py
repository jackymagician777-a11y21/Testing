import sys
import json
import os

def extract_shared_secret(mafile_path):
    """
    Extracts shared_secret from Steam MaFile
    Returns the shared_secret string or error message
    """
    try:
        # Check if file exists
        if not os.path.exists(mafile_path):
            print(f"ERROR:File not found: {mafile_path}")
            sys.exit(1)
        
        # Read and parse JSON
        with open(mafile_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # Extract shared_secret
        if 'shared_secret' not in data:
            print("ERROR:shared_secret not found in MaFile")
            sys.exit(1)
        
        shared_secret = data['shared_secret']
        
        if not shared_secret:
            print("ERROR:shared_secret is empty")
            sys.exit(1)
        
        # Return the secret
        print(f"SUCCESS:{shared_secret}")
        sys.exit(0)
        
    except json.JSONDecodeError as e:
        print(f"ERROR:Invalid JSON: {str(e)}")
        sys.exit(1)
    except Exception as e:
        print(f"ERROR:{str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("ERROR:MaFile path not provided")
        sys.exit(1)
    
    mafile_path = sys.argv[1]
    extract_shared_secret(mafile_path)
