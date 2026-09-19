"""Optional Higgsfield API adapter for OrdaX reference generation.

This adapter is deliberately non-authoritative: it may create visual references
or candidate media, but imported geometry must still pass the OrdaX furniture
pipeline. It never stores credentials in source control.

Environment:
  ORDAX_HIGGSFIELD_ENABLED=1
  HF_API_KEY_ID=...
  HF_API_KEY_SECRET=...
"""

from __future__ import annotations

import json
import os
from urllib import error, request


API_ROOT = "https://api.higgsfield.ai"
ENABLED_ENV = "ORDAX_HIGGSFIELD_ENABLED"
KEY_ID_ENV = "HF_API_KEY_ID"
KEY_SECRET_ENV = "HF_API_KEY_SECRET"


class HiggsfieldDisabled(RuntimeError):
    pass


class HiggsfieldConfigurationError(RuntimeError):
    pass


def enabled() -> bool:
    return os.getenv(ENABLED_ENV, "").strip() == "1"


def _credentials() -> tuple[str, str]:
    if not enabled():
        raise HiggsfieldDisabled(
            "Higgsfield integration is disabled. Set ORDAX_HIGGSFIELD_ENABLED=1 explicitly."
        )
    key_id = os.getenv(KEY_ID_ENV, "").strip()
    key_secret = os.getenv(KEY_SECRET_ENV, "").strip()
    if not key_id or not key_secret:
        raise HiggsfieldConfigurationError(
            "Higgsfield credentials are missing. Use environment variables; never commit them."
        )
    return key_id, key_secret


def _headers() -> dict[str, str]:
    key_id, key_secret = _credentials()
    return {
        "Authorization": f"Key {key_id}:{key_secret}",
        "Content-Type": "application/json",
        "User-Agent": "OrdaX-Furniture-Core/1",
    }


def submit(model_path: str, payload: dict, *, timeout: float = 60.0) -> dict:
    """Submit one Higgsfield API generation.

    model_path is the documented API model path, for example:
      marketing-studio/image
      alibaba/qwen-image-3/text-to-image

    3D Jutsu / Blender-plugin scene operations are intentionally not emulated
    here; use the official Higgsfield Blender integration for those.
    """
    normalized = model_path.strip().strip("/")
    if not normalized or ".." in normalized or "://" in normalized:
        raise ValueError("Invalid Higgsfield model path")

    body = json.dumps(payload).encode("utf-8")
    req = request.Request(
        f"{API_ROOT}/{normalized}",
        data=body,
        headers=_headers(),
        method="POST",
    )
    try:
        with request.urlopen(req, timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"Higgsfield request failed ({exc.code}): {detail}") from exc


def request_status(request_id: str, *, timeout: float = 30.0) -> dict:
    normalized = request_id.strip()
    if not normalized or "/" in normalized or "\\" in normalized:
        raise ValueError("Invalid Higgsfield request id")

    req = request.Request(
        f"{API_ROOT}/requests/{normalized}/status",
        headers=_headers(),
        method="GET",
    )
    try:
        with request.urlopen(req, timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except error.HTTPError as exc:
        detail = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"Higgsfield status request failed ({exc.code}): {detail}") from exc


def build_reference_request(
    prompt: str,
    *,
    resolution: str = "2k",
    aspect_ratio: str = "4:3",
) -> tuple[str, dict]:
    """Return a documented Marketing Studio Image request without executing it."""
    if not prompt.strip():
        raise ValueError("prompt is required")
    return (
        "marketing-studio/image",
        {
            "prompt": prompt.strip(),
            "quality": "high",
            "moderation": "auto",
            "resolution": resolution,
            "aspect_ratio": aspect_ratio,
            "enhance_prompt": False,
        },
    )


def ordax_import_contract(provider_result: dict, *, role: str) -> dict:
    """Wrap provider output as an unapproved OrdaX candidate."""
    return {
        "provider": "higgsfield",
        "role": role,
        "approved": False,
        "requires": [
            "real_dimension_normalization",
            "stable_ordax_ids",
            "part_isolation",
            "family_profile_validation",
            "pairwise_collision_validation",
            "human_visual_review",
        ],
        "provider_result": provider_result,
    }
