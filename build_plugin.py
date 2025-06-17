#!/usr/bin/env python3
"""
Script to build and package the AceJobSwitcher plugin.

This script:
1. Adds DownloadLink entries from the main AceJobSwitcher.json to the debug version
2. Wraps the debug JSON file content into an array
3. Creates a zip file of all contents in the bin/x64/Debug/ directory
"""

import json
import os
import shutil
import zipfile
from pathlib import Path


def main():
    # Define paths
    main_json_path = Path("FastJobSwitcher/AceJobSwitcher.json")
    debug_json_path = Path("FastJobSwitcher/bin/x64/Debug/AceJobSwitcher.json")
    debug_dir = Path("FastJobSwitcher/bin/x64/Debug")
    zip_path = debug_dir / "AceJobSwitcher.zip"
    
    print("Starting AceJobSwitcher build process...")
    
    # Step 1: Read the main JSON file to get DownloadLink entries
    print("1. Reading main AceJobSwitcher.json...")
    try:
        with open(main_json_path, 'r', encoding='utf-8') as f:
            main_json = json.load(f)
    except FileNotFoundError:
        print(f"Error: Could not find {main_json_path}")
        return False
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {main_json_path}: {e}")
        return False
    
    # Step 2: Read the debug JSON file
    print("2. Reading debug AceJobSwitcher.json...")
    try:
        with open(debug_json_path, 'r', encoding='utf-8') as f:
            debug_json = json.load(f)
    except FileNotFoundError:
        print(f"Error: Could not find {debug_json_path}")
        return False
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {debug_json_path}: {e}")
        return False
    
    # Step 3: Add DownloadLink entries to debug JSON
    print("3. Adding DownloadLink entries...")
    download_links = {
        "DownloadLinkInstall": main_json.get("DownloadLinkInstall"),
        "DownloadLinkTesting": main_json.get("DownloadLinkTesting"),
        "DownloadLinkUpdate": main_json.get("DownloadLinkUpdate")
    }
    
    # Add the download links to debug JSON
    for key, value in download_links.items():
        if value is not None:
            debug_json[key] = value
            print(f"   Added {key}: {value}")
    
    # Step 4: Wrap the JSON content in an array
    print("4. Wrapping JSON content in array...")
    json_array = [debug_json]
    
    # Step 5: Write the updated JSON back to file
    print("5. Writing updated JSON file...")
    try:
        with open(debug_json_path, 'w', encoding='utf-8') as f:
            json.dump(json_array, f, indent=2, ensure_ascii=False)
        print(f"   Successfully updated {debug_json_path}")
    except Exception as e:
        print(f"Error: Could not write to {debug_json_path}: {e}")
        return False
    
    # Step 6: Create zip file
    print("6. Creating zip file...")
    
    # Remove existing zip file if it exists
    if zip_path.exists():
        zip_path.unlink()
        print(f"   Removed existing {zip_path.name}")
    
    try:
        with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zipf:
            # Walk through all files and directories in debug_dir
            for root, dirs, files in os.walk(debug_dir):
                root_path = Path(root)
                
                # Skip the zip file itself to avoid recursion
                if root_path == debug_dir and zip_path.name in files:
                    files.remove(zip_path.name)
                
                for file in files:
                    file_path = root_path / file
                    # Calculate relative path from debug_dir
                    relative_path = file_path.relative_to(debug_dir)
                    zipf.write(file_path, relative_path)
                    print(f"   Added: {relative_path}")
        
        print(f"   Successfully created {zip_path}")
        print(f"   Zip file size: {zip_path.stat().st_size / 1024:.1f} KB")
        
    except Exception as e:
        print(f"Error: Could not create zip file: {e}")
        return False
    
    print("\n✅ Build process completed successfully!")
    return True


if __name__ == "__main__":
    success = main()
    if not success:
        exit(1) 